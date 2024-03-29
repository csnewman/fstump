grammar FStump;

entry
    : element* EOF
    ;

element
    : function #functionElement
    | globalDec #globalDecElement
    ;

globalDec
    : identifier ASSIGN numberLiteral SEMI #literalGlobalDec
    | identifier ASSIGN LBRACK numberLiteral RBRACK SEMI #blockGlobalDec
    | identifier ASSIGN LBRACE numberLiteral (COMMA numberLiteral)* RBRACE SEMI #arrayGlobalDec
    | identifier ASSIGN string SEMI #stringGlobalDec
    ;

function
    : FUNC identifier LPAREN functionArgs? RPAREN functionResult? LBRACE statement* RBRACE
    ;

functionArgs
    : identifier (COMMA identifier)*
    ;

functionResult
    : identifier (COMMA identifier)*
    ;

statement
    : NOP SEMI #nopStatement
    | LOCAL identifier SEMI #localStatement
    | name=identifier COLON #labelStatement
    | GOTO label=identifier SEMI #gotoStatement
    | GOTO LPAREN cond=identifier RPAREN lab=identifier SEMI #gotoCondStatement
    | CMP left=register right=register SEMI #cmpRegStatement
    | CMP left=register right=numberLiteral SEMI #cmpLitStatement
    | TEST left=register right=register SEMI #testRegStatement
    | TEST left=register right=numberLiteral SEMI #testLitStatement
    | dest=register ASSIGN val=identifier SEMI #loadStatement
    | dest=identifier ASSIGN val=register SEMI #storeStatement
    | dest=register ASSIGN val=numberLiteral SEMI #setStatement
    | MUL dest=register ASSIGN src=register SEMI #storeRegStatement
    | dest=register ASSIGN MUL src=register SEMI #loadRegStatement
    | dest=register ASSIGN left=register LSHIFT right=numberLiteral SEMI #lshiftStatement
    | dest=register LSHIFT left=register LSHIFT right=numberLiteral SEMI #lshiftStatement
    | dest=register ASSIGN left=register ADD right=register SEMI #addRegStatement
    | dest=register ADD_ASSIGN val=register SEMI #addAssignRegStatement
    | dest=register ASSIGN left=register ADD right=numberLiteral SEMI #addLitStatement
    | dest=register ADD_ASSIGN val=numberLiteral SEMI #addAssignLitStatement
    | dest=register ASSIGN left=register SUB right=register SEMI #subRegStatement
    | dest=register SUB_ASSIGN val=register SEMI #subAssignRegStatement
    | dest=register ASSIGN left=register SUB right=numberLiteral SEMI #subLitStatement
    | dest=register SUB_ASSIGN val=numberLiteral SEMI #subAssignLitStatement
    | (target=callResults ASSIGN)? identifier LPAREN callArgs? RPAREN SEMI #callStatement
    | RETURN SEMI #returnStatement
    | base=register LBRACK off=register RBRACK ASSIGN val=register SEMI #offsetRegStoreStatement
    | dest=register ASSIGN base=register LBRACK off=register RBRACK SEMI #offsetRegLoadStatement
    | dest=register ASSIGN AMP val=identifier SEMI #loadAddrStatement
    | dest=register ASSIGN left=register AMP right=numberLiteral SEMI #andLitStatement
    | dest=register ASSIGN left=register AMP right=register SEMI #andRegStatement
    | dest=register ASSIGN left=register OR right=numberLiteral SEMI #orLitStatement
    | dest=register ASSIGN left=register OR right=register SEMI #orRegStatement
    | dest=register ASSIGN src=register shift SEMI #shiftStatement
    | dest=register ASSIGN src=register SEMI #moveStatement
    ;

callArgs
    : callArg (COMMA callArg)*
    ;

callArg
    : identifier #idenCallArg
    | register #regCallArg
    | numberLiteral #litCallArg
    ;

callResults
    : register (COMMA register)*
    ;

identifier
    : IDENTIFIER
    ;

shift
    : ASR #asrShift
    | ROR #rorShift
    | RRC #rrcShift
    ;

register
    : ZERO #zeroRegister
    | R1 #r1Register
    | R2 #r2Register
    | R3 #r3Register
    | R4 #r4Register
    | LR #lrRegister
    | SF #sfRegister
    | PC #pcRegister
    ;

numberLiteral
    : DECIMAL_LITERAL #decimalNumberLiteral
    | HEX_LITERAL #hexNumberLiteral
    | OCT_LITERAL #octNumberLiteral
    | BINARY_LITERAL #binaryNumberLiteral
    | CHAR_LITERAL #charNumberLiteral
    ;

string
    : STRING
    ;

WS:             [ \t\r\n\u000C]+ -> channel(HIDDEN);
COMMENT:        '/*' .*? '*/'    -> channel(HIDDEN);
LINE_COMMENT:   '//' ~[\r\n]*    -> channel(HIDDEN);

FUNC:           'func';
GOTO:           'goto';
CMP:            'cmp';
TEST:           'test';
NOP:            'nop';
RETURN:         'return';
LOCAL:          'local';

ZERO:           'ZERO';
R1:             'R1';
R2:             'R2';
R3:             'R3';
R4:             'R4';
LR:             'LR';
SF:             'SF';
PC:             'PC';

MUL:            '*';
ASSIGN:         '=';
ADD:            '+';
ADD_ASSIGN:     '+=';
LSHIFT:         '<<';
LSHIFT_ASSIGN:  '<<=';
SUB:            '-';
SUB_ASSIGN:     '-=';


LPAREN:         '(';
RPAREN:         ')';
LBRACE:         '{';
RBRACE:         '}';
LBRACK:         '[';
RBRACK:         ']';
SEMI:           ';';
COMMA:          ',';
COLON:          ':';
AMP:            '&';
OR:             '|';

ASR:            'asr';
ROR:            'ror';
RRC:            'rrc';

IDENTIFIER:         Letter LetterOrDigit*;

DECIMAL_LITERAL:    ('0' | [1-9] (Digits? | '_'+ Digits)) [lL]?;
HEX_LITERAL:        '0' [xX] [0-9a-fA-F] ([0-9a-fA-F_]* [0-9a-fA-F])? [lL]?;
OCT_LITERAL:        '0' '_'* [0-7] ([0-7_]* [0-7])? [lL]?;
BINARY_LITERAL:     '0' [bB] [01] ([01_]* [01])? [lL]?;

CHAR_LITERAL:       '\'' (~['\\\r\n] | EscapeSequence) '\'';

STRING:             '"' (~['\\\r\n] | EscapeSequence)* '"';

fragment EscapeSequence
    : '\\' [btnfr"'\\]
    | '\\' ([0-3]? [0-7])? [0-7]
    | '\\' 'u'+ HexDigit HexDigit HexDigit HexDigit
    ;

fragment HexDigits
    : HexDigit ((HexDigit | '_')* HexDigit)?
    ;
fragment HexDigit
    : [0-9a-fA-F]
    ;
fragment Digits
    : [0-9] ([0-9_]* [0-9])?
    ;
fragment LetterOrDigit
    : Letter
    | [0-9]
    ;
fragment Letter
    : [a-zA-Z$_]
    | ~[\u0000-\u007F\uD800-\uDBFF]
    | [\uD800-\uDBFF] [\uDC00-\uDFFF] 
    ;