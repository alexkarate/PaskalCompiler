using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaskalCompiler
{
    class ModuleLexical
    {
    }
    enum ETokenType
    {
        Oper,
        Ident,
        Value
    };

    enum EOperator
    {
        star, // *
        slash, // /
        equal, // =
        comma, // ,
        semicolon, // ;
        programsy,
        ifsy,
        dosy,
        ofsy,
        orsy,
        tosy,
        endsy,
    }

    enum EVarType
    {
        vtInt = 0x1,
        vtReal = 0x2,
        vtString = 0x4,
        vtChar = 0x8
    };

    class CToken
    {
        public ETokenType _tt;
        public CToken()
        {
            _tt = ETokenType.Value;
        }
        public override string ToString() { return "Generic token"; }
    }

    class CValue : CToken
    {
        EVarType _vt;
        public CValue(EVarType type)
        {
            _tt = ETokenType.Value;
            _vt = type;
        }
        public override string ToString() { return "Constant"; }
    }

    class COperation : CToken
    {
        EOperator _vo;
        public COperation(EOperator op)
        {
            _vo = op;
        }
        public override string ToString() { return "Operation"; }
    }

    class CIdent : CToken
    {
        public CIdent()
        {

        }
        public override string ToString() { return "Ident"; }
    }
}
