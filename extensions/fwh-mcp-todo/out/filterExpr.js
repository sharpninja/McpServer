"use strict";
/**
 * Boolean filter expression for text filter: terms, !, &&, ||, and parentheses.
 * Examples: "plan || impl", "!plan", "plan && !impl", "(plan || impl) && !trip"
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.evaluate = evaluate;
exports.parseFilterText = parseFilterText;
const OP_AND = '&&';
const OP_OR = '||';
function tokenize(input) {
    const tokens = [];
    let i = 0;
    const n = input.length;
    while (i < n) {
        while (i < n && /\s/.test(input[i]))
            i++;
        if (i >= n)
            break;
        if (input.slice(i, i + 2) === OP_AND) {
            tokens.push({ type: '&&' });
            i += 2;
            continue;
        }
        if (input.slice(i, i + 2) === OP_OR) {
            tokens.push({ type: '||' });
            i += 2;
            continue;
        }
        if (input[i] === '(') {
            tokens.push({ type: '(' });
            i += 1;
            continue;
        }
        if (input[i] === ')') {
            tokens.push({ type: ')' });
            i += 1;
            continue;
        }
        if (input[i] === '!') {
            tokens.push({ type: '!' });
            i += 1;
            continue;
        }
        let term = '';
        while (i < n) {
            const rest = input.slice(i);
            if (rest.startsWith(OP_AND) || rest.startsWith(OP_OR) || input[i] === '(' || input[i] === ')' || input[i] === '!' || /\s/.test(input[i]))
                break;
            term += input[i];
            i += 1;
        }
        if (term)
            tokens.push({ type: 'term', value: term });
    }
    return tokens;
}
function parsePrimary(tokens, pos) {
    if (pos.i >= tokens.length)
        return null;
    const t = tokens[pos.i];
    if (t.type === '!') {
        pos.i += 1;
        const operand = parsePrimary(tokens, pos);
        return operand === null ? null : { kind: 'not', operand };
    }
    if (t.type === 'term') {
        pos.i += 1;
        return { kind: 'term', value: t.value };
    }
    if (t.type === '(') {
        pos.i += 1;
        const inner = parseOr(tokens, pos);
        if (pos.i < tokens.length && tokens[pos.i]?.type === ')') {
            pos.i += 1;
            return inner;
        }
        return inner;
    }
    return null;
}
/** and_expr = primary ( '&&' primary )* */
function parseAnd(tokens, pos) {
    let left = parsePrimary(tokens, pos);
    if (left === null)
        return null;
    while (pos.i < tokens.length && tokens[pos.i]?.type === '&&') {
        pos.i += 1;
        const right = parsePrimary(tokens, pos);
        if (right === null)
            return left;
        left = { kind: 'and', left, right };
    }
    return left;
}
/** or_expr = and_expr ( '||' and_expr )* */
function parseOr(tokens, pos) {
    let left = parseAnd(tokens, pos);
    if (left === null)
        return null;
    while (pos.i < tokens.length && tokens[pos.i]?.type === '||') {
        pos.i += 1;
        const right = parseAnd(tokens, pos);
        if (right === null)
            return left;
        left = { kind: 'or', left, right };
    }
    return left;
}
function parse(tokens) {
    if (tokens.length === 0)
        return null;
    const pos = { i: 0 };
    return parseOr(tokens, pos);
}
/** Returns true if searchable text matches the expression (case-insensitive). */
function evaluate(expr, searchable) {
    if (expr === null)
        return true;
    const hay = searchable.toLowerCase();
    function evalNode(e) {
        switch (e.kind) {
            case 'term':
                return hay.includes((e.value ?? '').toLowerCase());
            case 'not':
                return !evalNode(e.operand);
            case 'and':
                return evalNode(e.left) && evalNode(e.right);
            case 'or':
                return evalNode(e.left) || evalNode(e.right);
        }
    }
    return evalNode(expr);
}
/**
 * Parse filter string into an expression. If the string has no operators/parens,
 * treats space-separated words as AND (backward compatible).
 */
function parseFilterText(input) {
    const trimmed = input.trim();
    if (!trimmed)
        return null;
    const tokens = tokenize(trimmed);
    if (tokens.length === 0)
        return null;
    const hasOperators = tokens.some((t) => t.type === '!' || t.type === '&&' || t.type === '||' || t.type === '(' || t.type === ')');
    if (!hasOperators) {
        const terms = tokens.filter((t) => t.type === 'term').map((t) => t.value);
        if (terms.length === 0)
            return null;
        if (terms.length === 1)
            return { kind: 'term', value: terms[0] };
        let expr = { kind: 'term', value: terms[0] };
        for (let i = 1; i < terms.length; i++) {
            expr = { kind: 'and', left: expr, right: { kind: 'term', value: terms[i] } };
        }
        return expr;
    }
    return parse(tokens);
}
//# sourceMappingURL=filterExpr.js.map