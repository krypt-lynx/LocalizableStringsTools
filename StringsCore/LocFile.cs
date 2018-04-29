using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace StringsCore
{
    enum LocFileParserState
    {
        WaitingKey,
        InKey,
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

    enum LocFileParserStringSubstate
    {
        None,
        WaitingEscapeCode,
        UTF8Code,

        Done,
        SyntaxError
    }


    public class LocFile
    {
        List<Block> blocks;

        public List<KeyValuePair<string, string>> localizationPairs = new List<KeyValuePair<string, string>>();
        string currentKey = null;
        string currentValue = null;

        public LocFile(string path)
        {
            TextReader locReader = new StreamReader(path);

            this.Parse(locReader);

            locReader.Close();
        }

        private LocFileParserState parserState;
        private LocFileParserStringSubstate stringSubstate;
        private Stack<LocFileParserState> statesStack;

        private void Parse(TextReader reader)
        {
            parserState = LocFileParserState.WaitingKey;
            statesStack = new Stack<LocFileParserState>();
            int position = 1;
            int line = 1;

            int rawChar = reader.Read();
            while (rawChar != -1)
            {
                if (rawChar == '\n')
                {
                    position = 1;
                    line++;
                }
                else
                {
                    position++;
                }

                switch (parserState)
                {
                    case LocFileParserState.WaitingKey:
                        this.ProcessWaitingKey((char)rawChar);
                        break;
                    case LocFileParserState.InKey:
                        this.ProcessInKey((char)rawChar);
                        break;

                    case LocFileParserState.WaitingSeparator:
                        this.ProcessWaitingSeparator((char)rawChar);
                        break;

                    case LocFileParserState.WaitingValue:
                        this.ProcessWaitingValue((char)rawChar);
                        break;
                    case LocFileParserState.InValue:
                        this.ProcessInValue((char)rawChar);
                        break;

                    case LocFileParserState.WaitingSemicolon:
                        this.ProcessWaitingSemicolon((char)rawChar);
                        break;

                    case LocFileParserState.WaitingComment:
                        this.ProcessWaitingComment((char)rawChar);
                        break;
                    case LocFileParserState.LineComment:
                        this.ProcessLineComment((char)rawChar);
                        break;
                    case LocFileParserState.BlockComment:
                        this.ProcessBlockComment((char)rawChar);
                        break;
                    case LocFileParserState.WaitingQuitBlockComment:
                        this.ProcessWaitingQuitBlockComment((char)rawChar);
                        break;
                    default:
                        {
                            return; // oops
                        }
                        break;
                }

                if (parserState == LocFileParserState.SyntaxError)
                {
                    throw new Exception(string.Format("syntax error at line {0}, position {1}", line, position));
                }

                rawChar = reader.Read();
            }

            if (!(parserState == LocFileParserState.WaitingKey) && 
                !((parserState == LocFileParserState.LineComment) && (statesStack.Peek() == LocFileParserState.WaitingKey)))
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
                        this.statesStack.Push(this.parserState);
                        this.parserState = LocFileParserState.WaitingComment;
                    }
                    break;
                case '\"':
                    {
                        this.parserState = LocFileParserState.InValue;
                        this.InitReadString();
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = LocFileParserState.WaitingValue;
                        }
                        else
                        {
                            this.parserState = LocFileParserState.SyntaxError;
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
                        this.statesStack.Push(this.parserState);
                        this.parserState = LocFileParserState.WaitingComment;
                    }
                    break;
                case '=':
                    {
                        this.parserState = LocFileParserState.WaitingValue;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = LocFileParserState.WaitingSeparator;
                        }
                        else
                        {
                            this.parserState = LocFileParserState.SyntaxError;
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
                        this.parserState = LocFileParserState.WaitingComment;
                    }
                    break;
                case ';':
                    {
                        localizationPairs.Add(new KeyValuePair<string, string>(currentKey, currentValue));
                        this.parserState = LocFileParserState.WaitingKey;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = LocFileParserState.WaitingSemicolon;
                        }
                        else
                        {
                            this.parserState = LocFileParserState.SyntaxError;
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
                        this.statesStack.Push(this.parserState);
                        this.parserState = LocFileParserState.WaitingComment;
                    }
                    break;
                case '\"':
                    {
                        this.parserState = LocFileParserState.InKey;
                        this.InitReadString();
                    }
                    break;
                case ';':
                    {
                        this.parserState = LocFileParserState.WaitingKey;
                    }
                    break;
                default:
                    {
                        if (char.IsWhiteSpace(rawChar))
                        {
                            this.parserState = LocFileParserState.WaitingKey;
                        }
                        else
                        {
                            this.parserState = LocFileParserState.SyntaxError;
                        }
                    }
                    break;
            }
        }

        private void InitReadString()
        {
            this.stringSubstate = LocFileParserStringSubstate.None;
            this.currentString = new StringBuilder();
        }

        private StringBuilder currentString = new StringBuilder();
        private void ProcessInKey(char rawChar)
        {
            this.ProcessReadString(rawChar);
            // postprocess
            switch (stringSubstate)
            {
                case LocFileParserStringSubstate.Done:
                    {
                        currentKey = currentString.ToString();
                        parserState = LocFileParserState.WaitingSeparator;
                        // todo
                    }
                    break;
                case LocFileParserStringSubstate.SyntaxError:
                    {
                        parserState = LocFileParserState.SyntaxError;
                    }
                    break;
            }

        }

        private void ProcessInValue(char rawChar)
        {
            this.ProcessReadString(rawChar);
            // postprocess
            switch (stringSubstate)
            {
                case LocFileParserStringSubstate.Done:
                    {
                        currentValue = currentString.ToString();
                        parserState = LocFileParserState.WaitingSemicolon;
                        // todo
                    }
                    break;
                case LocFileParserStringSubstate.SyntaxError:
                    {
                        parserState = LocFileParserState.SyntaxError;
                    }
                    break;
            }
        }
        private void ProcessReadString(char rawChar)
        {
            switch (stringSubstate)
            {
                case LocFileParserStringSubstate.None:
                    {
                        this.ProcessStringChar(rawChar);
                    }
                    break;
                case LocFileParserStringSubstate.WaitingEscapeCode:
                    {
                        this.ProcessEscapeCode(rawChar);
                    }
                    break;
                case LocFileParserStringSubstate.UTF8Code:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        private void ProcessEscapeCode(char rawChar)
        {
            switch (rawChar)
            {
                case '\\':
                    {
                        currentString.Append('\\');
                        this.stringSubstate = LocFileParserStringSubstate.None;
                    }
                    break;
                case '\"':
                    {
                        currentString.Append('\"');
                        this.stringSubstate = LocFileParserStringSubstate.None;
                    }
                    break;
                case 'r':
                    {
                        currentString.Append('\r');
                        this.stringSubstate = LocFileParserStringSubstate.None;
                    }
                    break;
                case 'n':
                    {
                        currentString.Append('\n');
                        this.stringSubstate = LocFileParserStringSubstate.None;
                    }
                    break;
                case 't':
                    {
                        currentString.Append('\t');
                        this.stringSubstate = LocFileParserStringSubstate.None;
                    }
                    break;
                case 'U':
                    {
                        this.stringSubstate = LocFileParserStringSubstate.UTF8Code;
                    }
                    break;
                default:
                    {
                        throw new NotImplementedException();
                    }
                    break;
            }
        }

        private void ProcessStringChar(char rawChar)
        {
            switch (rawChar)
            {
                case '\\':
                    {
                        this.stringSubstate = LocFileParserStringSubstate.WaitingEscapeCode;
                    }
                    break;
                case '\"':
                    {
                        this.stringSubstate = LocFileParserStringSubstate.Done;
                    }
                    break;
                default:
                    {
                        this.stringSubstate = LocFileParserStringSubstate.None;
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
                        this.parserState = LocFileParserState.LineComment;
                    }
                    break;
                case '*':
                    {
                        this.parserState = LocFileParserState.BlockComment;
                    }
                    break;
                default:
                    {
                        this.parserState = LocFileParserState.SyntaxError;
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
                        this.parserState = LocFileParserState.WaitingQuitBlockComment;
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
                    }
                    break;
                default:
                    {
                        this.parserState = LocFileParserState.BlockComment;
                    }
                    break;
            }
        }
    }
}
