"""
Список всех исходных (русских) строк веба для перевода. docs/tasks/web-i18n.md, этап 5.

    powershell -File ArtvisDB/Scripts/i18n/dump-sources.ps1   # метаданные из ArtvisDev → db.json
    python ArtvisDB/Scripts/i18n/collect-sources.py           # + литералы FogSoft.Web → sources.json

sources.json — уникальные строки с пометкой, откуда они (menu, entity, column, action,
message, caption, code). Новые строки = те, которых нет в <lang>.tsv. Выходные файлы
(db.json, sources.json, menu_es.json) в git не кладутся.
"""
import json, os, re, sys, io, html
import xml.etree.ElementTree as ET
from collections import Counter
import importlib.util

D = os.path.dirname(os.path.abspath(__file__))
WEB = os.path.normpath(os.path.join(D, '..', '..', '..', 'FogSoft.Web'))
spec = importlib.util.spec_from_file_location("fu", os.path.join(WEB, "find-untranslated.py"))
fu = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fu)

CYR = re.compile(r"[А-Яа-яЁё]")
LIT = re.compile(r'(\$?@|@\$|\$)?"((?:[^"\\\n]|\\.|"")*)"')
ESC = {'n': '\n', 't': '\t', 'r': '\r', '"': '"', '\\': '\\', '0': '\0'}


def unescape(s):
    return re.sub(r'\\(.)', lambda m: ESC.get(m.group(1), m.group(1)), s)


src = {}


def add(s, kind):
    if s is None or not CYR.search(s):
        return
    src.setdefault(s, set()).add(kind)


RES_KEY = re.compile(r'Tr\.(?:T|Format)\(\s*(?:Merlin\.)?Properties\.Resources\.(\w+)')
resource_keys = set()
code_files = [p for p in fu.web_files() if p.endswith(('.razor', '.cs'))] + list(fu.core_files())
for path in code_files:
    raw = io.open(path, encoding='utf-8-sig').read()
    resource_keys.update(RES_KEY.findall(raw))
    text = fu.strip_comments(raw)
    for line in text.split('\n'):
        if re.search(r"\b(_?[Ll]og)\.(Info|Warn|Error|Debug|Fatal)", line):
            continue
        for m in LIT.finditer(line):
            prefix, body = m.group(1) or '', m.group(2)
            if '$' in prefix:
                continue
            add(body.replace('""', '"') if '@' in prefix else unescape(body), 'code')

# Ключи, которые процедуры переводят сами: dbo.fn_Translate(@язык, N'…') (этап 6).
SQL_KEY = re.compile(r"fn_Translate\(\s*@\w+\s*,\s*N'((?:[^']|'')*)'\s*\)", re.I)
for dp, dns, fns in os.walk(os.path.join(D, '..', '..', 'dbo')):
    for fn in fns:
        if fn.endswith('.sql') and fn != 'fn_Translate.sql':
            for key in SQL_KEY.findall(io.open(os.path.join(dp, fn), encoding='utf-8-sig').read()):
                add(key.replace("''", "'"), 'sql')

# Строки Client/Properties/Resources.resx, которые код пропускает через Tr.
resx = ET.parse(os.path.join(WEB, '..', 'Client', 'Properties', 'Resources.resx')).getroot()
for data in resx.findall('data'):
    if data.get('name') in resource_keys and data.find('value') is not None:
        add(data.find('value').text, 'code')

db = json.load(io.open(os.path.join(D, 'db.json'), encoding='utf-8'))
existing = {}
for r in db['menu']:
    add(r['t'], 'menu')
    if r.get('es') and r['t'] != '-':
        existing[r['t']] = r['es']
for r in db['entity']:
    add(r['t'], 'entity')
for r in db['attribute']:
    add(r['t'], 'column')
for r in db['action']:
    add(r['t'], 'action')
for r in db['message']:
    add(r['t'], 'message')
bad = 0
for r in db['passport']:
    x = r['t']
    try:
        for el in ET.fromstring(x).iter():
            add(el.get('caption'), 'caption')
    except ET.ParseError:
        bad += 1
        for m in re.finditer(r'caption\s*=\s*"([^"]*)"', x):
            add(html.unescape(m.group(1)), 'caption')

items = [{'s': s, 'k': sorted(k)} for s, k in sorted(src.items(), key=lambda kv: (sorted(kv[1])[0], kv[0]))]
json.dump(items, io.open(os.path.join(D, 'sources.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
json.dump(existing, io.open(os.path.join(D, 'menu_es.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
c = Counter(k for it in items for k in it['k'])
print('unique', len(items), 'chars', sum(len(i['s']) for i in items), 'badxml', bad, dict(c))
