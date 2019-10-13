grammar FStump;

entry
    : function* EOF
    ;

function
    : FUNC identifier LPAREN function_args? RPAREN return_type=type block
    ;

function_args
    : function_arg (COMMA function_arg)*
    ;

block
    : LBRACE statement* RBRACE
    ;

statement
    : type identifier SEMI #emptyVariableDefStatement
    | type identifier ASSIGN expression SEMI #assignVariableDefStatement
    | identifier ADD_ASSIGN expression SEMI #addVariableStatement
    | MUL LPAREN expression RPAREN ASSIGN expression SEMI #rawAssignStatement
    | block #blockStatement
    ;

expression
    : conditionalOrExpression
    ;

conditionalOrExpression
	:	conditionalAndExpression #bypassConditionalOrExpression
	|	left=conditionalOrExpression '||' right=conditionalAndExpression #defaultConditionalOrExpression
	;

conditionalAndExpression
	:	inclusiveOrExpression #bypassConditionalAndExpression
	|	left=conditionalAndExpression '&&' right=inclusiveOrExpression #defaultConditionalAndExpression
	;

inclusiveOrExpression
	:	exclusiveOrExpression #bypassInclusiveOrExpression
	|	left=inclusiveOrExpression '|' right=exclusiveOrExpression #defaultInclusiveOrExpression
	;

exclusiveOrExpression
	:	andExpression #bypassExclusiveOrExpression
	|	left=exclusiveOrExpression '^' right=andExpression #defaultExclusiveOrExpression
	;

andExpression
	:	equalityExpression #bypassAndExpression
	|	left=andExpression '&' right=equalityExpression #defaultAndExpression
	;

equalityExpression
	:	relationalExpression #bypassEqualityExpression
	|	left=equalityExpression '==' right=relationalExpression #equalEqualityExpression
	|	left=equalityExpression '!=' right=relationalExpression #notEqualEqualityExpression
	;

relationalExpression
	:	shiftExpression #bypassRelationalExpression
	|	left=relationalExpression '<' right=shiftExpression #ltRelationalExpression
	|	left=relationalExpression '>' right=shiftExpression #gtRelationalExpression
	|	left=relationalExpression '<=' right=shiftExpression #lteRelationalExpression
	|	left=relationalExpression '>=' right=shiftExpression #gteRelationalExpression
	;

shiftExpression
	:	additiveExpression #bypassShiftExpression
	|	left=shiftExpression '<' '<' right=additiveExpression #leftShiftExpression
	|	left=shiftExpression '>' '>' right=additiveExpression #rightShiftExpression
	|	left=shiftExpression '>' '>' '>' right=additiveExpression #specialRightShiftExpression
	;

additiveExpression
	:	multiplicativeExpression #bypassAdditiveExpression
	|	left=additiveExpression '+' right=multiplicativeExpression #addAdditiveExpression
	|	left=additiveExpression '-' right=multiplicativeExpression #subtractAdditiveExpression
	;

multiplicativeExpression
	:	castExpression #bypassMultiplicativeExpression
	|	left=multiplicativeExpression '*' right=castExpression #multMultiplicativeExpression
	|	left=multiplicativeExpression '/' right=castExpression #divMultiplicativeExpression
	|	left=multiplicativeExpression '%' right=castExpression #modMultiplicativeExpression
	;

castExpression
	: unaryExpression #bypassCastExpression
	| '(' type ')' unaryExpression #defaultCastExpression
	;

unaryExpression
	: baseExpression #bypassUnaryExpression
	| '-' baseExpression #negateUnaryExpression
    | '!' baseExpression #notUnaryExpression
	;

baseExpression
	: number_literal #numberBaseExpression
	| '(' expression ')' #subExpressionBaseExpression
	| identifier #variableBaseExpression
	| '--' identifier #preDecBaseExpression
	| '++' identifier #preIncBaseExpression
	| identifier '--' #postDecBaseExpression
	| identifier '++' #postIncBaseExpression
//	|	fieldAccess
//	|	methodInvocation
	;

function_arg
    : type identifier
    ;

type
    : VOID      #voidType
    | BOOL      #boolType
    | I16       #i16Type
    | I32       #i32Type
    | type '*'  #ptrType
    ;

identifier
    : IDENTIFIER
    ;

number_literal
    : DECIMAL_LITERAL
    | HEX_LITERAL
    | OCT_LITERAL
    | BINARY_LITERAL
    ;

WS:             [ \t\r\n\u000C]+ -> channel(HIDDEN);
FUNC:           'func';

VOID:           'void';
BOOL:           'bool';
I8:             'i8';
I16:            'i16';
I32:            'i32';
I64:            'i64';
I128:           'i128';

LPAREN:         '(';
RPAREN:         ')';
LBRACE:         '{';
RBRACE:         '}';
LBRACK:         '[';
RBRACK:         ']';
SEMI:           ';';
COMMA:          ',';
DOT:            '.';

ASSIGN:         '=';
GT:             '>';
LT:             '<';
BANG:           '!';
TILDE:          '~';
QUESTION:       '?';
COLON:          ':';
EQUAL:          '==';
LE:             '<=';
GE:             '>=';
NOTEQUAL:       '!=';
AND:            '&&';
OR:             '||';
INC:            '++';
DEC:            '--';
ADD:            '+';
SUB:            '-';
MUL:            '*';
DIV:            '/';
BITAND:         '&';
BITOR:          '|';
CARET:          '^';
MOD:            '%';
ADD_ASSIGN:     '+=';
SUB_ASSIGN:     '-=';
MUL_ASSIGN:     '*=';
DIV_ASSIGN:     '/=';
AND_ASSIGN:     '&=';
OR_ASSIGN:      '|=';
XOR_ASSIGN:     '^=';
MOD_ASSIGN:     '%=';
LSHIFT_ASSIGN:  '<<=';
RSHIFT_ASSIGN:  '>>=';
URSHIFT_ASSIGN: '>>>=';

IDENTIFIER:         Letter LetterOrDigit*;

DECIMAL_LITERAL:    ('0' | [1-9] (Digits? | '_'+ Digits)) [lL]?;
HEX_LITERAL:        '0' [xX] [0-9a-fA-F] ([0-9a-fA-F_]* [0-9a-fA-F])? [lL]?;
OCT_LITERAL:        '0' '_'* [0-7] ([0-7_]* [0-7])? [lL]?;
BINARY_LITERAL:     '0' [bB] [01] ([01_]* [01])? [lL]?;

BOOL_LITERAL:       'true'
            |       'false'
            ;

CHAR_LITERAL:       '\'' (~['\\\r\n] | EscapeSequence) '\'';

STRING_LITERAL:     '"' (~["\\\r\n] | EscapeSequence)* '"';


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