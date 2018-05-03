using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace strings2csv
{
    static class CSV
    {
        const char CSV_TEXT_QUOTES = '"';
        const char CSV_RC = '\r';
        const char CSV_ROW_DELIMETER = '\n';
        const char CSV_CELL_DELIMETER = ',';

        const string CSV_ROW_WRITE_DELIMETER = "\r\n";

        public static List<List<string>> ToList(TextReader reader)
        {
            List<List<string>> result = new List<List<string>>();

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
                            result.Add(row);
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
                result.Add(row);
            }

            return result;
        }

        public static string ToString(List<List<string>> table)
        {
            StringWriter writer = new StringWriter();
            Write(writer, table);
            writer.Flush();
            return writer.ToString();
        }

        public static void Write(TextWriter writer, List<List<string>> table)
        {
            writer.Write("\uFEFF"); // utf8 BOM marker
            for (int i = 0; i < table.Count; i++)
            {
                List<string> row = table[i];
                for (int j = 0; j < row.Count; j++)
                {
                    string cell = row[j];

                    writeCSVString(writer, cell);

                    if (j != row.Count-1)
                        writer.Write(CSV_CELL_DELIMETER);
                }
                if (i != table.Count - 1)
                    writer.Write(CSV_ROW_WRITE_DELIMETER);
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
