"""Shared treatment of English terms in prose, headings, code and figures."""
import re

LATIN = re.compile(r'https?://[^\s]+|[./]?[A-Za-z][A-Za-z0-9_]*(?:[./:+#-][A-Za-z0-9_]+)*#?')

def segments(text):
    start = 0
    for match in LATIN.finditer(text):
        if match.start() > start:
            yield text[start:match.start()], False
        yield match.group(), True
        start = match.end()
    if start < len(text):
        yield text[start:], False
