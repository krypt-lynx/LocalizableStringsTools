using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace csv
{
    static class CSV
    {
        const char CSV_TEXT_QUOTES = '"';
        const char CSV_RC = '\r';
        const char CSV_ROW_DELIMETER = '\n';
        const char CSV_CELL_DELIMETER = ',';

        const string CSV_ROW_WRITE_DELIMETER = "\r\n";

        public static IEnumerable<List<string>> Read(TextReader reader)
        {
            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();

            bool isQouted = false;
            bool isQoutedStop = false;
            bool isCellStart = true;
            bool somethingReaded = false;

            int oldCh = -1;
            int ch = reader.Read();
            while ((ch != -1) && ((char)ch == CSV_RC)) // Пропускаем все \r
                ch = reader.Read();

            while (ch != -1)
            {
                somethingReaded = true;

                if (isQouted)
                {
                    if (isQoutedStop)
                    {
                        isQoutedStop = false;
                        if ((char)ch != CSV_TEXT_QUOTES)
                            isQouted = false;
                    }
                    else
                    {
                        if ((char)ch == CSV_TEXT_QUOTES)
                            isQoutedStop = true;
                    }
                }

                if (isCellStart)
                {
                    isCellStart = false;
                    if ((char)ch == CSV_TEXT_QUOTES)
                        isQouted = true;
                }

                if (isQouted)
                {
                    cell.Append((char)ch);
                }
                else
                {
                    if (((char)ch == CSV_CELL_DELIMETER) || ((char)ch == CSV_ROW_DELIMETER))
                    {
                        // Ячейка прочитанна
                        isCellStart = true;
                        isQouted = false;
                        isQoutedStop = false;

                        row.Add(readCSVString(cell.ToString()));
                        cell = new StringBuilder();
                        if ((char)ch == CSV_ROW_DELIMETER)
                        {
                            yield return row;
                            row = new List<string>();
                            somethingReaded = false;
                        }
                    }
                    else
                    {
                        cell.Append((char)ch);
                    }
                }


                oldCh = ch;
                ch = reader.Read();

                while ((ch != -1) && ((char)ch == CSV_RC)) // Пропускаем все \r
                    ch = reader.Read();
            }

            if (somethingReaded)
            {
                row.Add(readCSVString(cell.ToString()));
                yield return row;
            }
        }

        public static string ToString(IEnumerable<IEnumerable<string>> table)
        {
            StringWriter writer = new StringWriter();
            Write(writer, table);
            writer.Flush();
            return writer.ToString();
        }

        public static void Write(TextWriter writer, IEnumerable<IEnumerable<string>> table)
        {
            bool firstRow = true;
            bool firstCell;
            foreach (var row in table)
            {
                if (!firstRow)
                {
                    writer.Write(CSV_ROW_WRITE_DELIMETER);
                }
                firstRow = false;

                firstCell = true;
                foreach (var cell in row)
                {
                    if (!firstCell)
                    {
                        writer.Write(CSV_CELL_DELIMETER);
                    }
                    firstCell = false;
                    writeCSVString(writer, cell);
                }
            }
        }

        static string readCSVString(string value)
        {
            if (value.Length >= 2)
            {
                if ((value[0] == CSV_TEXT_QUOTES) &&
                     (value[value.Length - 1] == CSV_TEXT_QUOTES))
                {
                    return value.Substring(1, value.Length - 2).Replace(CSV_TEXT_QUOTES.ToString() + CSV_TEXT_QUOTES.ToString(), CSV_TEXT_QUOTES.ToString());
                }
                else
                {
                    return value;
                }
            }
            else
            {
                return value;
            }
        }

        static void writeCSVString(TextWriter writer, string value)
        {
            string writeValue;

            if (value.Contains(CSV_CELL_DELIMETER) ||
                value.Contains(CSV_ROW_DELIMETER) ||
                value.Contains(CSV_TEXT_QUOTES))
            {
                writeValue = null;
                writeValue = CSV_TEXT_QUOTES + value.Replace(CSV_TEXT_QUOTES.ToString(), CSV_TEXT_QUOTES.ToString() + CSV_TEXT_QUOTES.ToString()) + CSV_TEXT_QUOTES;
            }
            else
            {
                writeValue = value;
            }
            writer.Write(writeValue);
        }
    }
}