#!/usr/bin/env python3
"""Splits long C# lines that hold several statements into one statement per line, and expands one-line blocks
({ a; b; }) into indented blocks. Only whitespace and line breaks change: before writing, every file is checked so that
its code without whitespace (strings and comments kept exactly) is identical to the original, so behaviour cannot change.

usage: python3 Tools/Format/split_long_lines.py [--limit 150] [--check] FILE_OR_DIR ...
  --check  report the long lines that would change, write nothing
"""
import pathlib, sys

LIMIT = 150
ACCESSORS = {'get', 'set', 'init', 'private set', 'protected set', 'internal set', 'private init'}

# ---- tokenizer: code characters, and opaque pieces (strings, chars, comments) ----
def read_string(s, i):
    """s[i] starts a string literal (possibly with $ / @ prefixes). Returns the end index (exclusive)."""
    j = i; interp = verbatim = False
    while s[j] in '$@':
        interp |= s[j] == '$'; verbatim |= s[j] == '@'; j += 1
    assert s[j] == '"'
    j += 1
    while j < len(s):
        c = s[j]
        if verbatim and c == '"':
            if j + 1 < len(s) and s[j + 1] == '"': j += 2; continue
            return j + 1
        if not verbatim and c == '\\': j += 2; continue
        if not verbatim and c == '"': return j + 1
        if interp and c == '{':
            if j + 1 < len(s) and s[j + 1] == '{': j += 2; continue
            j = skip_code_until_brace(s, j + 1); continue
        j += 1
    raise ValueError('unterminated string')

def skip_code_until_brace(s, j):
    """Inside an interpolation hole: skip code up to the matching '}' (returns index after it)."""
    depth = 0
    while j < len(s):
        c = s[j]
        if c in '"' or (c in '$@' and j + 1 < len(s) and s[j + 1] in '"$@'):
            j = read_string(s, j); continue
        if c == "'": j = read_char(s, j); continue
        if c in '({[': depth += 1
        elif c in ')]': depth -= 1
        elif c == '}':
            if depth == 0: return j + 1
            depth -= 1
        j += 1
    raise ValueError('unterminated interpolation')

def read_char(s, i):
    j = i + 1
    if s[j] == '\\': j += 2
    else: j += 1
    while s[j] != "'": j += 1
    return j + 1

def tokens(s):
    """List of (kind, text): kind 'c' = one code char, 'o' = opaque literal/comment."""
    out = []; i = 0
    while i < len(s):
        c = s[i]
        if s.startswith('//', i):
            out.append(('o', s[i:])); break
        if s.startswith('/*', i):
            j = s.index('*/', i + 2) + 2; out.append(('o', s[i:j])); i = j; continue
        if c == '"' or (c in '$@' and i + 1 < len(s) and s[i + 1] in '"$@'):
            j = read_string(s, i); out.append(('o', s[i:j])); i = j; continue
        if c == "'":
            j = read_char(s, i); out.append(('o', s[i:j])); i = j; continue
        out.append(('c', c)); i += 1
    return out

def normalized(text):
    """The file without whitespace outside strings/comments (preprocessor lines kept as they are)."""
    parts = []
    for line in text.split('\n'):
        if line.lstrip().startswith('#'): parts.append(line.strip()); continue
        for kind, t in tokens(line):
            parts.append(t if kind == 'o' else ('' if t.isspace() else t))
    return ''.join(parts)

# ---- formatting ----
def text_of(toks): return ''.join(t for _, t in toks)

def split_statements(toks):
    """Split at ';' that is not inside (), [] or {} (the ';' stays with its statement)."""
    out, cur, depth = [], [], 0
    for k, t in toks:
        cur.append((k, t))
        if k == 'c':
            if t in '([{': depth += 1
            elif t in ')]}': depth -= 1
            elif t == ';' and depth == 0: out.append(cur); cur = []
    if cur and text_of(cur).strip(): out.append(cur)
    elif cur and out: out[-1].extend(cur)
    return out

def block_end(toks, i):
    depth = 0
    for j in range(i, len(toks)):
        k, t = toks[j]
        if k != 'c': continue
        if t in '([{': depth += 1
        elif t in ')]}':
            depth -= 1
            if depth == 0: return j
    return -1

def is_statement_block(inner):
    parts = [text_of(p).strip().rstrip(';').strip() for p in split_statements(inner)]
    parts = [p for p in parts if p]
    if not any(text_of(p).count(';') or True for p in split_statements(inner)): return False
    has_semicolon = any(k == 'c' and t == ';' and d == 0 for k, t, d in with_depth(inner))
    if not has_semicolon: return False
    return not all(p in ACCESSORS or p.startswith('get=>') or p.startswith('set=>') for p in parts)

def with_depth(toks):
    depth = 0
    for k, t in toks:
        if k == 'c' and t in ')]}': depth -= 1
        yield k, t, depth
        if k == 'c' and t in '([{': depth += 1

def emit(toks, indent, lines):
    """Writes one statement (possibly containing expandable blocks) as lines."""
    cur = ''; i = 0
    while i < len(toks):
        k, t = toks[i]
        if k == 'c' and t == '{':
            j = block_end(toks, i)
            inner = toks[i + 1:j]
            if j > 0 and is_statement_block(inner):
                if cur.strip(): lines.append(indent + cur.strip())
                lines.append(indent + '{')
                for stmt in split_statements(inner): emit(stmt, indent + '    ', lines)
                cur = '}'; i = j + 1
                # A new statement right after the block goes on its own line (not else/catch/finally or a closing ')').
                rest = text_of(toks[i:]).lstrip()
                word = rest.split('(')[0].split(' ')[0]
                if rest and (rest[0].isalpha() or rest[0] == '_') and word not in ('else', 'catch', 'finally', 'while'):
                    lines.append(indent + cur); cur = ''
                    while i < len(toks) and toks[i][0] == 'c' and toks[i][1].isspace(): i += 1
                continue
        cur += t; i += 1
    if cur.strip():
        if cur.strip().startswith('}') and lines and False: pass
        lines.append(indent + cur.strip())

def format_line(line):
    indent = line[:len(line) - len(line.lstrip())]
    toks = tokens(line.strip())
    lines = []
    for stmt in split_statements(toks): emit(stmt, indent, lines)
    return lines

def format_text(text, limit):
    out = []; changed = 0; in_block_comment = False
    for line in text.split('\n'):
        stripped = line.strip()
        if in_block_comment:
            out.append(line); in_block_comment = '*/' not in line; continue
        if '/*' in line and '*/' not in line.split('/*', 1)[1]:
            out.append(line); in_block_comment = True; continue
        if len(line) <= limit or stripped.startswith('#') or stripped.startswith('///') or stripped.startswith('//'):
            out.append(line); continue
        try: new = format_line(line)
        except (ValueError, IndexError, AssertionError): out.append(line); continue
        if len(new) > 1: changed += 1
        out.extend(new)
    return '\n'.join(out), changed

def main():
    args = sys.argv[1:]; check = '--check' in args; limit = LIMIT
    if '--limit' in args: limit = int(args[args.index('--limit') + 1])
    paths = [pathlib.Path(a) for a in args if not a.startswith('--') and not a.isdigit()]
    files = [f for p in paths for f in (sorted(p.rglob('*.cs')) if p.is_dir() else [p])]
    total = 0
    for f in files:
        text = f.read_text()
        new, changed = format_text(text, limit)
        if not changed: continue
        if normalized(new) != normalized(text):
            print('SKIPPED (would change code):', f); continue
        total += changed
        print(f'{f}: {changed} lines split')
        if not check: f.write_text(new)
    print('total', total)

if __name__ == '__main__':
    main()
