"""
Ищет в FogSoft.Web русский текст, который не проходит через перевод (Tr.T / Tr.Format).
docs/tasks/web-i18n.md, этап 3.

    python FogSoft.Web/find-untranslated.py            # список file:line: текст
    python FogSoft.Web/find-untranslated.py --summary  # только счётчики по файлам

Не считаются: комментарии (//, /* */, @* *@, <!-- -->, ///), строки внутри Tr.T(...) и
первого аргумента Tr.Format(...), строки логов (Log./_log.) и строки с пометкой
// i18n-ok (осознанно не переводится: ключ данных, текст для разработчика).
Код возврата 1, если что-то найдено.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.abspath(__file__))
SKIP_DIRS = {"bin", "obj", "lib", "node_modules"}
EXTS = (".razor", ".cs", ".js")
CYR = re.compile(r"[А-Яа-яЁё]")

# Строковый литерал C#: обычный, @"", $"", $@"" / @$"".
STRING = r'(?:\$?@|@\$|\$)?"(?:[^"\\\n]|\\.|"")*"'
TRANSLATED = re.compile(r'Tr\.(?:T|Format)\(\s*' + STRING)


def strip_comments(text):
    def blank(m):
        return re.sub(r"[^\n]", " ", m.group(0))
    text = re.sub(r"@\*.*?\*@", blank, text, flags=re.S)
    text = re.sub(r"<!--.*?-->", blank, text, flags=re.S)
    text = re.sub(r"/\*.*?\*/", blank, text, flags=re.S)
    # // до конца строки, но не внутри строкового литерала и не в URL (http://)
    out = []
    for line in text.split("\n"):
        m = re.search(r'(?<![:"])//(?!.*i18n-ok)', line)
        if m and line[:m.start()].count('"') % 2 == 0:
            line = line[:m.start()]
        out.append(line)
    return "\n".join(out)


def scan(path):
    with open(path, encoding="utf-8-sig") as f:
        raw = f.read()
    text = strip_comments(raw)
    found = []
    for no, (line, orig) in enumerate(zip(text.split("\n"), raw.split("\n")), 1):
        if "i18n-ok" in orig or re.search(r"\b(_?[Ll]og)\.(Info|Warn|Error|Debug|Fatal)", line):
            continue
        rest = TRANSLATED.sub("", line)
        if CYR.search(rest):
            found.append((no, orig.strip()))
    return found


def main():
    summary = "--summary" in sys.argv
    total = 0
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        for name in sorted(filenames):
            if not name.endswith(EXTS):
                continue
            path = os.path.join(dirpath, name)
            found = scan(path)
            if not found:
                continue
            total += len(found)
            rel = os.path.relpath(path, os.path.dirname(ROOT))
            if summary:
                print(f"{len(found):4}  {rel}")
            else:
                for no, line in found:
                    print(f"{rel}:{no}: {line}")
    print(f"Итого строк с непереведённым русским: {total}")
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
