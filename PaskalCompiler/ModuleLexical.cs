using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaskalCompiler
{
    class ModuleLexical
    {
        ModuleIO io;
        StringBuilder currentSymbol = new StringBuilder();
        char bufferedChar = '\0';
        ETokenType predictedSymbol = ETokenType.Ident;
        EVarType predictedType = EVarType.vtInt;
        public ModuleLexical(ModuleIO io)
        {
            this.io = io;
        }

        public CToken NextSym()
        {
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
                if(currentSymbol.Length == 0)
                {
                    EOperator op;
                    if (IdentBegin(c))
                    {
                        currentSymbol.Append(c);
                        predictedSymbol = ETokenType.Ident;
                    }
                    else if (SingularSymbol(c, out op))
                    {
                        return new COperation(op);
                    }
                    else if(MultipleSymbol(c))
                    {
                        currentSymbol.Append(c);
                        predictedSymbol = ETokenType.Oper;
                    }
                    else if(c == '\'')
                    {
                        currentSymbol.Append(c);
                        predictedSymbol = ETokenType.Value;
                        predictedType = EVarType.vtChar;
                    }
                    else if(char.IsNumber(c))
                    {
                        currentSymbol.Append(c);
                        predictedSymbol = ETokenType.Value;
                        predictedType = EVarType.vtInt;
                    }
                    else if(!char.IsWhiteSpace(c))
                        io.RecordError(new IncorrectCharacterError(c));
                }
                else
                {
                    if (char.IsWhiteSpace(c))
                        break;
                    if (predictedSymbol == ETokenType.Ident)
                    {
                        if (Ident(c))
                        {
                            currentSymbol.Append(c);
                        }
                        else
                        {
                            bufferedChar = c;
                            break;
                        }
                    }
                    else if(predictedSymbol == ETokenType.Oper)
                    {
                        EOperator t;
                        if(MultipleSymbol(c))
                        {
                            currentSymbol.Append(c);
                            if (!IsOperator(currentSymbol.ToString(), out t))
                            {
                                currentSymbol.Remove(currentSymbol.Length - 1, 1);
                                bufferedChar = c;
                                break;
                            }
                        }
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
                            if (c == '\'')
                            {
                                currentSymbol.Append(c);
                                break;
                            }
                                
                            if (predictedType == EVarType.vtChar && currentSymbol.Length >= 2)
                                predictedType = EVarType.vtString;
                            currentSymbol.Append(c);
                        }
                        else if(predictedType == EVarType.vtInt || predictedType == EVarType.vtReal)
                        {
                            if (char.IsDigit(c))
                                currentSymbol.Append(c);
                            else if(c == '.' || char.ToLower(c) == 'e')
                            {
                                if(predictedType == EVarType.vtInt)
                                {
                                    currentSymbol.Append(char.ToLower(c));
                                    predictedType = EVarType.vtReal;
                                }
                                else
                                {
                                    c = char.ToLower(c);
                                    string value = currentSymbol.ToString();
                                    if (c == 'e' && value.Contains('.') && !value.Contains('e'))
                                        currentSymbol.Append(c);
                                    else
                                    {
                                        bufferedChar = c;
                                        break;
                                    }
                                }
                            }
                            else if(c == '-' || c == '+'  && predictedType == EVarType.vtReal)
                            {
                                if (currentSymbol[currentSymbol.Length - 1] == 'e')
                                    currentSymbol.Append(c);
                                else
                                {
                                    bufferedChar = c;
                                    break;
                                }
                            }
                            else
                            {
                                bufferedChar = c;
                                break;
                            }
                        }
                    }
                    else
                        io.RecordError(new IncorrectCharacterError(c));
                }
                c = io.NextChar();
            }
            if (currentSymbol.Length == 0)
                return CToken.empty;
            if (predictedSymbol == ETokenType.Ident)
            {
                EOperator op;
                bool logicalRet;
                string value = currentSymbol.ToString();
                if(IsReserved(value, out op))
                {
                    return new COperation(op);
                }
                else if(IsLogical(value, out logicalRet))
                {
                    return new CValue(EVarType.vtBoolean, logicalRet);
                }
                else
                {
                    if (value.Length <= 80)
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
                if(predictedType == EVarType.vtChar)
                {
                    if (currentSymbol.Length == 2)
                        return new CValue(EVarType.vtString, string.Empty);
                    else
                        return new CValue(EVarType.vtChar, currentSymbol[1]);
                }
                else if(predictedType == EVarType.vtString)
                {
                    return new CValue(EVarType.vtString, currentSymbol.ToString().Substring(1, currentSymbol.Length - 2));
                }
                else if(predictedType == EVarType.vtInt)
                {
                    int value = Convert.ToInt32(currentSymbol.ToString());
                    return new CValue(EVarType.vtInt, value);
                }
                else if(predictedType == EVarType.vtReal)
                {
                    double value = Convert.ToDouble(currentSymbol.ToString().Replace('.', ','));
                    return new CValue(EVarType.vtReal, value);
                }
            }
            return CToken.empty;
        }
        class IncorrectCharacterError : ErrorInformation
        {
            public IncorrectCharacterError(char c) : base(string.Format("incorrect character '{0}'", c)) { }
        }

        bool IsReserved(string value, out EOperator op)
        {

            switch(value)
            {
                case "Program":
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
                case "integer":
                    op = EOperator.integersy;
                    return true;
                case "boolean":
                    op = EOperator.booleansy;
                    return true;
                case "real":
                    op = EOperator.realsy;
                    return true;
                case "char":
                    op = EOperator.charsy;
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

                default:
                    op = EOperator.star;
                    return false;
            }
        }

        bool IsOperator(string value, out EOperator op)
        {
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

        bool IsLogical(string value, out bool read)
        {
            switch(value)
            {
                case "True":
                    read = true;
                    return true;
                case "False":
                    read = false;
                    return true;
                default:
                    read = false;
                    return false;
            }
        }
        bool SingularSymbol(char c, out EOperator op)
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

        bool MultipleSymbol(char c)
        {
            return c == ':' || c == '=' || c == '<' || c == '>' || c == '.';
        }

        bool IdentBegin(char c)
        {
            return char.IsLetter(c) || c == '_';
        }
        
        bool Ident(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
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
        //ofsy,
        //orsy,
        //tosy,
        endsy,

        integersy,
        booleansy,
        realsy,
        charsy,
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
        public override string ToString() { return "Generic token"; }
        public static CToken empty { get => new CToken(); }
    }

    class CValue : CToken
    {
        public EVarType _vt;
        public readonly object value;
        public CValue(EVarType type, object value)
        {
            _tt = ETokenType.Value;
            _vt = type;
            this.value = value;
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
    }

    class COperation : CToken
    {
        public EOperator _vo;
        public COperation(EOperator op)
        {
            _tt = ETokenType.Oper;
            _vo = op;
        }
        public override string ToString() { return string.Format("Operator({0})", _vo.ToString()); }
    }

    class CIdentificator : CToken
    {
        public string identName;
        public CIdentificator(string name)
        {
            identName = name;
            _tt = ETokenType.Ident;
        }
        public override string ToString() { return string.Format("Ident({0})", identName); }
    }
}
