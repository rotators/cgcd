using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace cgcd
{
    class Program
    {
        static Dictionary<string, string> data = new Dictionary<string, string>();

        enum Tok
        {
            WORD,    // example
            ASSIGN,  // =
            NEWLINE, // \n
            COMMENT, 
            NUMBER,  // 1234
            EOF
        }

        class Token
        {
            public Tok type;
            public object literal;
        }

        class Tokenizer
        {
            int i = 0;
            int len;
            int line = 1;
            string buffer;
            List<Token> tokens;

            public List<Token> Tokenize(string file)
            {
                tokens = new List<Token>();
                buffer = File.ReadAllText(file)+ "\r\n";
                len = buffer.Length - 1;
                while (i != len)
                {
                    Scan();
                }
                tokens.Add(new Token
                {
                    type = Tok.EOF
                });
                return tokens;
            }

            public void Scan()
            {
                void _n(Tok type)
                    => tokens.Add(new Token { type = type });

                var t = Read();
                switch (t)
                {
                    case '/':
                        {
                            var p = Peek();
                            if (p == '/')
                            {
                                Read();
                                Comment();
                            }
                        }
                        break;
                    case '=':
                        {
                            switch (Peek())
                            {
                                default: _n(Tok.ASSIGN); break;
                            }
                        }
                        break;
                    case '\r': break;
                    case '\t': break;
                    case '\n': line++; _n(Tok.NEWLINE); break;
                    case ' ': break;
                    default:
                        {
                            if (char.IsDigit(t) || (t == '-' && char.IsDigit(Peek())))
                            {
                                var number = ReadUntil(null, new char[] { ' ', '(', ')', ',', '+', '*', '\r', '\n' });
                                tokens.Add(
                                    new Token { type = Tok.NUMBER, literal = int.Parse(number) }
                                );
                            }
                            else if (char.IsLetter(t))
                            {
                                var word = ReadUntil(null, new char[] { ' ', '(', ')', ',', '\r', '\n' });
                                tokens.Add(
                                        new Token { type = Tok.WORD, literal = word }
                                    );
                            }
                            else
                            {
                                throw new Exception($"unable to read '{t}' at pos {i}, line {line}");
                            }
                            break;
                        }
                }
            }

            public void Comment()
            {
                i++;
                tokens.Add(new Token { type = Tok.COMMENT, literal = ReadUntil("comment", new char[] { '\r', '\n' }) });
            }

            public string ReadUntil(string t, char c)
                => ReadUntil(t, new char[] { c });

            public string ReadUntil(string t, char[] c)
            {
                int spos = i;
                var sb = new StringBuilder();
                sb.Append(buffer[i - 1]);
                while (true)
                {
                    if (c.Any(x => x == Peek()))
                    {
                        return sb.ToString();
                    }
                    sb.Append(Read());
                    if (EOF())
                        throw new Exception($"unterminated {t}, starting from {spos}");
                }
            }


            public bool Digit()
                => char.IsDigit(buffer[i]);

            public bool EOF()
                => i == len;

            public char Read()
                => buffer[i++];

            public char Peek()
                => buffer[i];

        }

        static void Main(string[] args)
        {
            // Force english language, for exceptions
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var ch = Path.Combine(dir, "character.txt");

            if (args.Length > 0)
                ch = args[0];

            if (!File.Exists(ch))
            {
                Console.WriteLine($"Unable to find {ch}");
                return;
            }

            var t = new Tokenizer();
            var tokens = t.Tokenize(ch);
            var i = 0;
            while(i < tokens.Count)
            {
                var token = tokens[i];
                if (token.type == Tok.WORD && tokens[i + 1].type == Tok.ASSIGN)
                {
                    string v = "";
                    if (tokens[i + 2].literal is int)
                        v = ((int)tokens[i + 2].literal).ToString();
                    else
                        v = (string)tokens[i + 2].literal;

                    data[(string)token.literal] = v;
                }
                i++;
            }

            Console.WriteLine($"Parsed {ch}");

            byte[] _int(string s, int min, int max)
            { 
                var num = int.Parse(data[s]);
                byte[] bytes = BitConverter.GetBytes(num);
                Array.Reverse(bytes, 0, bytes.Length);

                return bytes;
            }

            int _int_is(string s)
                => int.Parse(data[s]);

            var outFile = Path.Combine(dir, "character.gcd");
            if (args.Length > 1)
                outFile = args[1];

            if (File.Exists(outFile))
                File.Delete(outFile);

            Console.WriteLine($"Writing {outFile}...");

            int numBytes = 0;
            using (var br = new BinaryWriter(File.Open(outFile, FileMode.CreateNew)))
            {
                var max = new byte[] { 0xff, 0xff, 0xff, 0xff };
                var _null = new byte[] { 0x00, 0x00, 0x00, 0x00 };

                br.Write(_null);
                br.Write(_int("st", 0, 10));
                br.Write(_int("pe", 0, 10));
                br.Write(_int("en", 0, 10));
                br.Write(_int("ch", 0, 10));
                br.Write(_int("in", 0, 10));
                br.Write(_int("ag", 0, 10));
                br.Write(_int("lk", 0, 10));
                for (var x = 0; x < 26; x++) // 0x20 - 0x84
                    br.Write(_null);
                br.Write(_int("age", 1, 99));
                br.Write(_int("gender", 0,1));
                for (var x = 0; x < 35; x++) // 0x90 - 0x118
                    br.Write(_null);
                for (var x = 0; x < 18; x++) // all skills
                    br.Write(_int("sk_" + x, 0, 300));

                // 0x164-0x16F
                br.Write(_null);
                br.Write(_null);
                br.Write(_null);
                br.Write(_null);
                var name = Encoding.ASCII.GetBytes(data["name"]).ToList();
                while (name.Count < 32)
                    name.Add((byte)0);
                br.Write(name.ToArray());   
                br.Write(_int_is("tag_1") == -1 ? max : _int("tag_1", 0, 17));
                br.Write(_int_is("tag_2") == -1 ? max : _int("tag_2", 0, 17));
                br.Write(_int_is("tag_3") == -1 ? max : _int("tag_3", 0, 17));
                br.Write(_int_is("tag_4") == -1 ? max : _int("tag_4", 0, 17));
                br.Write(_int_is("trait_1") == -1 ? max : _int("trait_1", 0, 15));
                br.Write(_int_is("trait_2") == -1 ? max : _int("trait_2", 0, 15));
                br.Write(_int("char_points", 0, 99));
                br.Write(_null);
                br.Write(_int("style", 0 , 10));
                numBytes = (int)br.BaseStream.Position;
            }

            Console.WriteLine($"Done! Wrote {numBytes} bytes to {outFile}");

            }
    }
}
