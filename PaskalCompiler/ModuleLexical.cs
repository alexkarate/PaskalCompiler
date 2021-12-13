using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PaskalCompiler
{
    class ModuleLexical
    {
        public readonly ModuleIO io;
        StringBuilder currentSymbol = new StringBuilder();
        char bufferedChar = '\0';
        ETokenType predictedSymbol = ETokenType.Ident;
        EVarType predictedType = EVarType.vtInt;
        public ModuleLexical(ModuleIO io)
        {
            this.io = io;
        }

        void PredictSymbol(char c)
        {
            if (IsIdentBegin(c)) 
            {
                currentSymbol.Append(c);
                predictedSymbol = ETokenType.Ident;
            }
            else if (IsAllowedSymbol(c)) 
            {
                currentSymbol.Append(c);
                predictedSymbol = ETokenType.Oper;
            }
            else if (c == '\'') 
            {
                currentSymbol.Append(c);
                predictedSymbol = ETokenType.Value;
                predictedType = EVarType.vtChar;
            }
            else if (char.IsNumber(c)) 
            {
                currentSymbol.Append(c);
                predictedSymbol = ETokenType.Value;
                predictedType = EVarType.vtInt;
            }
            else if (!char.IsWhiteSpace(c)) // Skip whitespace
                io.RecordError(new IncorrectCharacterException(c));
        }

        public CToken NextSym()
        {
            // Buffer from previous iteration
            currentSymbol.Clear();
            char c;
            if(bufferedChar != '\0')
            {
                c = bufferedChar;
                bufferedChar = '\0';
            }
            else
                c = io.NextChar();

            while(c != '\0')
            {
                // If this is a new symbol
                if(currentSymbol.Length == 0)
                {
                    PredictSymbol(c);
                }
                else
                {
                    if (char.IsWhiteSpace(c) && predictedType != EVarType.vtChar && predictedType != EVarType.vtString) // On whitespace resolve the symbol for non-string types
                        break;

                    if (predictedSymbol == ETokenType.Ident)
                    {
                        if (ShouldAcceptIntoIdent(c))
                            currentSymbol.Append(c);
                        else
                        {
                            bufferedChar = c;
                            break;
                        }
                    }
                    else if(predictedSymbol == ETokenType.Oper)
                    {
                        if(ShouldAcceptIntoOper(c))
                            currentSymbol.Append(c);
                        else
                        {
                            bufferedChar = c;
                            break;
                        }
                        
                    }
                    else if(predictedSymbol == ETokenType.Value)
                    {
                        if(predictedType == EVarType.vtChar || predictedType == EVarType.vtString)
                        {
                            if (ShouldAcceptIntoString(c))
                                currentSymbol.Append(c);
                            else
                            {
                                bufferedChar = c;
                                break;
                            }    
                            
                        }
                        else if(predictedType == EVarType.vtInt || predictedType == EVarType.vtReal)
                        {
                            if(ShouldAcceptIntoNumber(c))
                                currentSymbol.Append(c);
                            else
                            {
                                bufferedChar = c;
                                break;
                            }
                        }
                    }
                    else
                        io.RecordError(new IncorrectCharacterException(c));
                }
                c = io.NextChar();
            }
            if (currentSymbol.Length == 0) // If we are resolving an empty string, then we are finished
                return CToken.empty;

            if (predictedSymbol == ETokenType.Ident)
            {
                EOperator op;
                string value = currentSymbol.ToString();
                if(IsReserved(value, out op))
                {
                    return new COperation(op);
                }
                else
                {
                    if (value.Length <= 80) // Record only the first 80 characters in identifier
                        return new CIdentificator(value);
                    else
                        return new CIdentificator(value.Substring(0, 80));
                }
            }
            else if(predictedSymbol == ETokenType.Oper)
            {
                EOperator op;
                string value = currentSymbol.ToString();
                if (IsOperator(value, out op))
                {
                    return new COperation(op);
                }
                else
                    throw new ApplicationException("Tried to add a symbol that doesn't exist");
            }
            else if(predictedSymbol == ETokenType.Value)
            {
                if(predictedType == EVarType.vtChar || predictedType == EVarType.vtString)
                {
                    ReplacePairsOfQuotes(currentSymbol);

                    if (currentSymbol.Length == 2)    // If the value is "''" then return an empty string
                        return new CValue(EVarType.vtString, string.Empty);
                    else if(currentSymbol.Length > 3) // Return string
                        return new CValue(EVarType.vtString, currentSymbol.ToString().Substring(1, currentSymbol.Length - 2));
                    else                              // Return char
                        return new CValue(EVarType.vtChar, currentSymbol[1]);
                }
                else if(predictedType == EVarType.vtInt)
                {
                    int value = Convert.ToInt32(currentSymbol.ToString());
                    return new CValue(EVarType.vtInt, value);
                }
                else if(predictedType == EVarType.vtReal)
                {
                    float value = Convert.ToSingle(currentSymbol.ToString().Replace('.', ','));
                    return new CValue(EVarType.vtReal, value);
                }
            }
            return CToken.empty;
        }

        bool ShouldAcceptIntoIdent(char c)
        {
            return IsIdentBegin(c) || char.IsNumber(c);
        }
        bool ShouldAcceptIntoOper(char c)
        {
            if (IsMultipleSymbol(c))// If current is operation, then accept all symbols, as long as the new operation exists
                return IsOperator(currentSymbol.ToString() + c);
            else
                return false;
        }

        bool ShouldAcceptIntoString(char c)
        {
            if (c == '\'')
                return true; //Accept all adjacent quotes
            else if (currentSymbol[currentSymbol.Length - 1] == '\'') // Accept only pairs of quotes ('' => '). Otherwise resolve the string
            {
                int quoteCount = 0;
                for (int i = currentSymbol.Length - 1; i > 0; i--)
                {
                    if (currentSymbol[i] == '\'')
                        quoteCount++;
                    else
                        break;
                }
                return quoteCount % 2 == 0;
            }
            return true;
        }
        bool ShouldAcceptIntoNumber(char c)
        {
            if (char.IsDigit(c))
                return true;

            if (c == '.' || char.ToLower(c) == 'e')
            {
                if (predictedType == EVarType.vtInt) // If we encountered a dot or an e while current is an integer, then change current to real
                {
                    predictedType = EVarType.vtReal;
                    return true;
                }
                else // If current is real, then the only allowed format is (int).(int)e(int). Otherwise, resolve the symbol.
                {
                    c = char.ToLower(c);
                    string value = currentSymbol.ToString();
                    return c == 'e' && value.Contains(".") && !value.Contains("e");
                }
            }
            else if (c == '-' || c == '+' && predictedType == EVarType.vtReal) // If we encounter a + or - after e, then it's part of the number. Otherwise, resolve the symbol
                return currentSymbol[currentSymbol.Length - 1] == 'e';
            else
                return false;
        }

        void ReplacePairsOfQuotes(StringBuilder str)
        {
            str.Replace("''", "'", 1, str.Length - 2);
        }

        bool IsReserved(string value, out EOperator op)
        {
            switch(value.ToLower())
            {
                case "program":
                    op = EOperator.programsy;
                    return true;
                case "end":
                    op = EOperator.endsy;
                    return true;
                case "begin":
                    op = EOperator.beginsy;
                    return true;

                case "var":
                    op = EOperator.varsy;
                    return true;

                case "div":
                    op = EOperator.divsy;
                    return true;
                case "mod":
                    op = EOperator.modsy;
                    return true;

                case "not":
                    op = EOperator.notsy;
                    return true;
                case "and":
                    op = EOperator.andsy;
                    return true;
                case "or":
                    op = EOperator.orsy;
                    return true;
                case "xor":
                    op = EOperator.xorsy;
                    return true;

                case "while":
                    op = EOperator.whilesy;
                    return true;
                case "do":
                    op = EOperator.dosy;
                    return true;
                case "if":
                    op = EOperator.ifsy;
                    return true;
                case "then":
                    op = EOperator.thensy;
                    return true;
                case "else":
                    op = EOperator.elsesy;
                    return true;
                case "of":
                    op = EOperator.ofsy;
                    return true;
                case "to":
                    op = EOperator.tosy;
                    return true;

                default:
                    op = EOperator.star;
                    return false;
            }
        }

        bool IsOperator(string value, out EOperator op)
        {
            if (value.Length == 1 && IsSingularSymbol(value[0], out op))
                return true;
            switch(value)
            {
                case ":=":
                    op = EOperator.assignSy;
                    return true;
                case ":":
                    op = EOperator.colon;
                    return true;
                case ">":
                    op = EOperator.greater;
                    return true;
                case "<":
                    op = EOperator.less;
                    return true;
                case ">=":
                    op = EOperator.greaterequals;
                    return true;
                case "<=":
                    op = EOperator.lessequals;
                    return true;
                case "=":
                    op = EOperator.equals;
                    return true;
                case "<>":
                    op = EOperator.notequals;
                    return true;

                case ".":
                    op = EOperator.dot;
                    return true;
                default:
                    op = EOperator.star;
                    return false;
            }
               
        }
        bool IsOperator(string s)
        {
            EOperator op;
            return IsOperator(s, out op);
        }
        bool IsSingularSymbol(char c, out EOperator op)
        {
            switch (c)
            {
                case ';':
                    op = EOperator.semicolon;
                    return true;
                case ',':
                    op = EOperator.comma;
                    return true;
                case '*':
                    op = EOperator.star;
                    return true;
                case '/':
                    op = EOperator.slash;
                    return true;
                case '+':
                    op = EOperator.plus;
                    return true;
                case '-':
                    op = EOperator.minus;
                    return true;
                case '(':
                    op = EOperator.openBr;
                    return true;
                case ')':
                    op = EOperator.closeBr;
                    return true;
                default:
                    op = EOperator.star;
                    return false;
            }
        }
        bool IsSingularSymbol(char c)
        {
            EOperator op;
            return IsSingularSymbol(c, out op);
        }

        bool IsMultipleSymbol(char c)
        {
            return c == ':' || c == '=' || c == '<' || c == '>' || c == '.';
        }
        bool IsAllowedSymbol(char c)
        {
            return IsMultipleSymbol(c) || IsSingularSymbol(c);
        }

        bool IsIdentBegin(char c)
        {
            return Regex.IsMatch(c.ToString(), @"^[a-zA-Z_]+$");
        }
    }
    enum ETokenType
    {
        None,
        Oper,
        Ident,
        Value
    };

    enum EOperator
    {
        star, // *
        slash, // /
        plus, // +
        minus, // -
        openBr, // (
        closeBr, // )
        comma, // ,
        semicolon, // ;
        dot, // .

        colon, // :
        assignSy, // :=

        divsy, // div
        modsy, // mod

        greater, // >
        less, // <
        equals, // =
        notequals, // <>
        greaterequals, // >=
        lessequals, // <=

        notsy,
        andsy,
        orsy,
        xorsy,

        programsy,
        beginsy,
        varsy,
        ifsy,
        thensy,
        elsesy,
        dosy,
        whilesy,
        ofsy,
        tosy,
        endsy,
    }

    enum EVarType
    {
        vtInt = 0x1,
        vtReal = 0x2,
        vtString = 0x4,
        vtChar = 0x8,
        vtBoolean = 0x16
    };

    class CToken
    {
        public ETokenType _tt;
        public CToken()
        {
            _tt = ETokenType.None;
        }
        public override string ToString() { return "Empty token"; }
        public static CToken empty { get => new CToken(); }
        public override bool Equals(object obj)
        {
            CToken token = obj as CToken;
            if (token == null)
                return false;

            if (token._tt != _tt)
                return false;

            switch (_tt)
            {
                case ETokenType.Ident:
                    return ((CIdentificator)token).Equals(this);
                case ETokenType.Oper:
                    return ((COperation)token).Equals(this);
                case ETokenType.Value:
                    return ((CValue)token).Equals(this);
                default:
                    return true;
            }
        }
    }

    class CValue : CToken
    {
        public EVarType _vt;
        public readonly object value;
        public CValue(EVarType type, object value)
        {
            _tt = ETokenType.Value;
            _vt = type;
            if (value != null)
                this.value = value;
            else
                this.value = "Any";
        }
        public override string ToString()
        {
            string format;
            switch (_vt)
            {
                case EVarType.vtChar: 
                    format = "Character ('{0}')";
                    break;
                case EVarType.vtString: 
                    format = "String (\"{0}\")";
                    break;
                case EVarType.vtInt:
                    format = "Integer ({0})";
                    break;
                case EVarType.vtReal:
                    format = "Real ({0})";
                    break;
                case EVarType.vtBoolean:
                    format = "Boolean ({0})";
                    break;
                default:
                    format = "Impossible to get to";
                    break;
            }

            return string.Format(format, value.ToString()); 
        }
        public override bool Equals(object obj)
        {
            CValue token = obj as CValue;
            if (token == null)
                return false;

            return token._vt == _vt;
        }
    }

    class COperation : CToken
    {
        public EOperator _vo;
        public COperation(EOperator op)
        {
            _tt = ETokenType.Oper;
            _vo = op;
        }
        public override string ToString() { return string.Format("Operator ({0})", _vo.ToString()); }

        public override bool Equals(object obj)
        {
            COperation token = obj as COperation;
            if (token == null)
                return false;

            return token._vo == _vo;
        }
        public bool IsBinary()
        {
            return _vo == EOperator.orsy || _vo == EOperator.andsy;
        }
        public bool IsEquals()
        {
            return _vo == EOperator.equals || _vo == EOperator.notequals;
        }
        public bool IsIntegerDivision()
        {
            return _vo == EOperator.divsy || _vo == EOperator.modsy;
        }
        public bool IsAdditive()
        {
            return _vo == EOperator.plus || _vo == EOperator.minus || _vo == EOperator.orsy;
        }
        public bool IsMultiplicative()
        {
            return _vo == EOperator.star || _vo == EOperator.slash || _vo == EOperator.divsy || _vo == EOperator.modsy || _vo == EOperator.andsy;
        }
        public bool IsRelative()
        {
            return _vo == EOperator.equals || _vo == EOperator.notequals || _vo == EOperator.greater || _vo == EOperator.greaterequals || _vo == EOperator.less || _vo == EOperator.lessequals;
        }

    }

    class CIdentificator : CToken
    {
        public string identName;
        public CIdentificator(string name)
        {
            identName = name;
            _tt = ETokenType.Ident;
        }
        public override string ToString() 
        {
            if (!string.IsNullOrEmpty(identName))
                return string.Format("Identifier ({0})", identName);
            else
                return "Identifier";
        }

        public override bool Equals(object obj)
        {
            CIdentificator token = obj as CIdentificator;
            if (token == null)
                return false;
            return true;
        }
    }
    class LexerException : CompilerException
    {
        public LexerException() : base("Generic lexical exception.") { }
        public LexerException(string s) : base(s) { }
    }
    class IncorrectCharacterException : LexerException
    {
        public IncorrectCharacterException(char c) : base(string.Format("incorrect character '{0}'", c)) { }
    }
}
