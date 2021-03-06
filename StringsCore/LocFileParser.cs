using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// Идеи по улучшению
// Флаг-индикатор был ли использован символ и его обработка
// Поддержка вложенности
// Разбить на модули

namespace StringsCore
{
    enum ParserState
    {
        WaitingKey,
        InKey,
        InInlineKey,
        WaitingSeparator,
        WaitingValue,
        InValue,
        WaitingSemicolon,
        WaitingComment,
        LineComment,
        BlockComment,
        WaitingQuitBlockComment,

        Done,
        SyntaxError
    }

    enum ParserStringSubstate
    {
        None,
        WaitingEscapeCode,
        UTF8Code,

        Done,
        DoneNotConsumed,
        SyntaxError
    }

    public class LocFileParser : IDisposable
    {

        public LocFile document { get; private set; }
        private Stack<LocTreeEntry> docStack = null;

        string currentKey = null;
        string currentValue = null;

        StringBuilder currentText = new StringBuilder();
        StringBuilder currentComment = new StringBuilder();

        TextReader reader = null;

        public LocFileParser(string path)
        {
            reader = new StreamReader(path);
        }


        public void Dispose()
        {
            reader.Close();
            reader.Dispose();
        }

        public LocFile Parse()
        {

            document = new LocFile();
            this.ParseInternal(reader);
            document.FinalizeTree();

            reader.Close();

            return document;
        }

        

        private ParserState parserState;
        private ParserStringSubstate stringSubstate;
        private Stack<ParserState> statesStack;
        private StringBuilder unicodeSequence;
        private int expectedUnicodeChars;

        private void ParseInternal(TextReader reader)
        {
            docStack = new Stack<LocTreeEntry>();
            docStack.Push(document);

            parserState = ParserState.WaitingKey;
            statesStack = new Stack<ParserState>();
            


            int position = 1;
            int line = 1;

            int rawChar = reader.Read();
            while (rawChar != -1)
            {
                if (rawChar == '\n')
                {
                    position = 0;
                    line++;
                }
                else
                {
                    position++;
                }

                switch (parserState)
                {
                    case ParserState.WaitingKey:
                        this.ProcessWaitingKey((char)rawChar);
                        break;
                    case ParserState.InKey:
                        this.ProcessInKey((char)rawChar);
                        break;
                    case ParserState.InInlineKey:
                        this.ProcessInInlineKey((char)rawChar);
                        break;

                    case ParserState.WaitingSeparator:
                        this.ProcessWaitingSeparator((char)rawChar);
                        break;

                    case ParserState.WaitingValue:
                        this.ProcessWaitingValue((char)rawChar);
                        break;
                    case ParserState.InValue:
                        this.ProcessInValue((char)rawChar);
                        break;

                    case ParserState.WaitingSemicolon:
                        this.ProcessWaitingSemicolon((char)rawChar);
                        break;

                    case ParserState.WaitingComment:
                        this.ProcessWaitingComment((char)rawChar);
                        break;
                    case ParserState.LineComment:
                        this.ProcessLineComment((char)rawChar);
                        break;
                    case ParserState.BlockComment:
                        this.ProcessBlockComment((char)rawChar);
                        break;
                    case ParserState.WaitingQuitBlockComment:
                        this.ProcessWaitingQuitBlockComment((char)rawChar); // +
                        break;
                    default:
                        {
                            return; // oops
                        }
                }

                if (parserState == ParserState.SyntaxError)
                {
                    throw new Exception(string.Format("syntax error at line {0}, position {1}", line, position));
                }

                rawChar = reader.Read();
            }

            if (!(parserState == ParserState.WaitingKey) && 
                !((parserState == ParserState.LineComment) && (statesStack.Peek() == ParserState.WaitingKey)))
            {
                return; //oops
            }
        }

        private void ProcessWaitingValue(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        AppendTextBlockIfNeeded();
                        this.statesStack.Push(this.parserState);
                        this.parserState = ParserState.WaitingComment;
                    }
                    break;
                case '\"':
                    {
                        AppendTextBlockIfNeeded();
                        this.parserState = ParserState.InValue;
                        this.InitReadString();
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.currentText.Append(rawChar);
                            this.parserState = ParserState.WaitingValue;
                        }
                        else
                        {
                            this.parserState = ParserState.SyntaxError;
                        }
                    }
                    break;
            }
        }

        private void ProcessWaitingSeparator(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        AppendTextBlockIfNeeded();
                        this.statesStack.Push(this.parserState);
                        this.parserState = ParserState.WaitingComment;
                    }
                    break;
                case '=':
                    {
                        AppendTextBlockIfNeeded();
                        docStack.Peek().Append(new SeparatorBlock());
                        this.parserState = ParserState.WaitingValue;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = ParserState.WaitingSeparator;
                            this.currentText.Append(rawChar);
                        }
                        else
                        {
                            this.parserState = ParserState.SyntaxError;
                        }
                    }
                    break;
            }
        }

        private void ProcessWaitingSemicolon(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        this.statesStack.Push(this.parserState);
                        this.parserState = ParserState.WaitingComment;
                    }
                    break;
                case ';':
                    {
                        AppendTextBlockIfNeeded();
                        docStack.Peek().Append(new SemicolonBlock());
                        docStack.Pop();
                        this.parserState = ParserState.WaitingKey;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = ParserState.WaitingSemicolon;
                            this.currentText.Append(rawChar);
                        }
                        else
                        {
                            this.parserState = ParserState.SyntaxError;
                        }
                    }
                    break;
            }
        }

        private void ProcessWaitingKey(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        AppendTextBlockIfNeeded();
                        this.statesStack.Push(this.parserState);
                        this.parserState = ParserState.WaitingComment;
                    }
                    break;
                case '\"':
                    {
                        AppendTextBlockIfNeeded();
                        this.parserState = ParserState.InKey;
                        this.InitReadString();
                        LocTreeEntry pair = new LocPairBlock();
                        docStack.Peek().Append(pair);
                        docStack.Push(pair);
                    }
                    break;
                case ';':
                    {
                        AppendTextBlockIfNeeded();
                        docStack.Peek().Append(new SemicolonBlock());
                        this.parserState = ParserState.WaitingKey;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = ParserState.WaitingKey;
                            this.currentText.Append(rawChar);
                        }
                        else if (char.IsLetterOrDigit(rawChar))
                        {
                            AppendTextBlockIfNeeded();
                            this.parserState = ParserState.InInlineKey;
                            this.InitReadInlineString();
                            LocTreeEntry pair = new LocPairBlock();
                            docStack.Peek().Append(pair);
                            docStack.Push(pair);
                            this.ProcessReadInlineString(rawChar);
                        }
                        else
                        {
                            this.parserState = ParserState.SyntaxError;
                        }
                    }
                    break;
            }
        }

        private void AppendTextBlockIfNeeded()
        {
            if (this.currentText.Length > 0)
            {
                docStack.Peek().Append(new TextBlock(this.currentText.ToString()));
                this.currentText = new StringBuilder();
            }
        }

        private void InitReadString()
        {
            this.stringSubstate = ParserStringSubstate.None;
            this.currentString = new StringBuilder();
        }

        private void InitReadInlineString()
        {
            this.stringSubstate = ParserStringSubstate.None;
            this.currentString = new StringBuilder();
        }

        private StringBuilder currentString = new StringBuilder();

        private void ProcessInKey(char rawChar)
        {
            this.ProcessReadString(rawChar);
            // postprocess
            switch (stringSubstate)
            {
                case ParserStringSubstate.Done:
                    {
                        currentKey = currentString.ToString();
                        parserState = ParserState.WaitingSeparator;
                        docStack.Peek().Append(new KeyBlock(currentKey));
                    }
                    break;
                case ParserStringSubstate.SyntaxError:
                    {
                        parserState = ParserState.SyntaxError;
                    }
                    break;
            }
        }

        private void ProcessInInlineKey(char rawChar)
        {
            this.ProcessReadInlineString(rawChar);
            if (stringSubstate == ParserStringSubstate.DoneNotConsumed)
            {
                currentKey = currentString.ToString();
                parserState = ParserState.WaitingSeparator;
                docStack.Peek().Append(new KeyBlock(currentKey, true));

                this.ProcessWaitingSeparator((char)rawChar); // TODO: костыль
            }
        }

        private void ProcessInValue(char rawChar)
        {
            this.ProcessReadString(rawChar);
            // postprocess
            switch (stringSubstate)
            {
                case ParserStringSubstate.Done:
                    {
                        currentValue = currentString.ToString();
                        parserState = ParserState.WaitingSemicolon;
                        docStack.Peek().Append(new ValueBlock(currentValue));
                    }
                    break;
                case ParserStringSubstate.SyntaxError:
                    {
                        parserState = ParserState.SyntaxError;
                    }
                    break;
            }
        }
        private void ProcessReadString(char rawChar)
        {
            switch (stringSubstate)
            {
                case ParserStringSubstate.None:
                    {
                        this.ProcessStringChar(rawChar);
                    }
                    break;
                case ParserStringSubstate.WaitingEscapeCode:
                    {
                        this.ProcessEscapeCode(rawChar);
                    }
                    break;
                case ParserStringSubstate.UTF8Code:
                    {
                        this.ProcessUnicodeChar(rawChar);                        
                    }
                    break;
                default:
                    throw new Exception("internal inconsistency");
            }
        }

        private void ProcessReadInlineString(char rawChar)
        {
            switch (stringSubstate)
            {
                case ParserStringSubstate.None:
                    {
                        this.ProcessInlineStringChar(rawChar);
                    }
                    break;
                default:
                    throw new Exception("internal inconsistency");
            }
        }

        private void ProcessInlineStringChar(char rawChar)
        {
            if (char.IsLetterOrDigit(rawChar) || // todo: propper characters list
                rawChar == '_' ||
                rawChar == '.' ||
                rawChar == ':')
            {
                currentString.Append(rawChar);
                stringSubstate = ParserStringSubstate.None;
            }
            else
            {
                stringSubstate = ParserStringSubstate.DoneNotConsumed;
            }
        }

        private void ProcessUnicodeChar(char rawChar)
        {
            if (Uri.IsHexDigit(rawChar)) // todo: найти метод, расположенный в чуть менее абсурдном месте
            {
                unicodeSequence.Append(rawChar);
                expectedUnicodeChars--;
            }
            else
            {
                // todo: fallback
                throw new InvalidDataException(string.Format("Invalid unicode sequence: \\{0}{1} ", unicodeSequence, rawChar));
            }

            if (expectedUnicodeChars <= 0)
            {
                uint ucharcode;
                if (!uint.TryParse(unicodeSequence.ToString(), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ucharcode))
                {
                    throw new InvalidDataException(string.Format("Invalid unicode sequence: \\{0} ", unicodeSequence));
                }
                currentString.Append((char)ucharcode);

                stringSubstate = ParserStringSubstate.None;
            }
            else
            {
                stringSubstate = ParserStringSubstate.UTF8Code;
            }
        }

        private void ProcessEscapeCode(char rawChar)
        {
            switch (rawChar)
            {
                case '\\':
                    {
                        currentString.Append('\\');
                        this.stringSubstate = ParserStringSubstate.None;
                    }
                    break;
                case '\"':
                    {
                        currentString.Append('\"');
                        this.stringSubstate = ParserStringSubstate.None;
                    }
                    break;
                case 'r':
                    {
                        currentString.Append('\r');
                        this.stringSubstate = ParserStringSubstate.None;
                    }
                    break;
                case 'n':
                    {
                        currentString.Append('\n');
                        this.stringSubstate = ParserStringSubstate.None;
                    }
                    break;
                case 't':
                    {
                        currentString.Append('\t');
                        this.stringSubstate = ParserStringSubstate.None;
                    }
                    break;
                case 'U':
                    {
                        this.stringSubstate = ParserStringSubstate.UTF8Code;
                        unicodeSequence = new StringBuilder();
                        expectedUnicodeChars = 4;
                    }
                    break;
                default:
                    {
                        throw new InvalidDataException(string.Format("Unexpected escape code: {0}", rawChar));
                    }
            }
        }

        private void ProcessStringChar(char rawChar)
        {
            switch (rawChar)
            {
                case '\\':
                    {
                        this.stringSubstate = ParserStringSubstate.WaitingEscapeCode;
                    }
                    break;
                case '\"':
                    {
                        this.stringSubstate = ParserStringSubstate.Done;
                    }
                    break;
                default:
                    {
                        this.stringSubstate = ParserStringSubstate.None;
                        currentString.Append(rawChar);
                    }
                    break;
            }
        }

        private void ProcessWaitingComment(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        this.parserState = ParserState.LineComment;
                    }
                    break;
                case '*':
                    {
                        this.parserState = ParserState.BlockComment;
                    }
                    break;
                default:
                    {
                        this.parserState = ParserState.SyntaxError;
                    }
                    break;
            }
        }

        private void ProcessLineComment(char rawChar)
        {
            switch (rawChar)
            {
                case '\n':
                    {
                        this.parserState = this.statesStack.Pop();
                        docStack.Peek().Append(new LineCommentBlock(currentComment.ToString()));
                        currentComment = new StringBuilder();
                    }
                    break;
                default:
                    {
                        currentComment.Append(rawChar);
                    }
                    break;
            }
        }

        private void ProcessBlockComment(char rawChar)
        {
            switch (rawChar)
            {
                case '*':
                    {
                        AppendTextBlockIfNeeded();
                        this.parserState = ParserState.WaitingQuitBlockComment;
                    }
                    break;
                default:
                    {
                        currentComment.Append(rawChar);
                    }
                    break;
            }
        }

        private void ProcessWaitingQuitBlockComment(char rawChar)
        {
            switch (rawChar)
            {
                case '/':
                    {
                        this.parserState = this.statesStack.Pop();
                        docStack.Peek().Append(new BlockCommentBlock(currentComment.ToString()));
                        currentComment = new StringBuilder();
                    }
                    break;
                default:
                    {
                        this.parserState = ParserState.BlockComment;
                        currentComment.Append(rawChar);
                    }
                    break;
            }
        }
    }
}
