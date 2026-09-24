--[[
pagebreak - convert \pagebreak and \newpage into real page breaks

The official pandoc-ext/pagebreak filter, shipped with HomerScribe because
PANDOC STILL HAS NO NATIVE PAGE BREAK: jgm/pandoc issue #1934 remains open and
the feature is described as "on the roadmap".

Without this, a paragraph reading \pagebreak survives into docx as the literal
text "\pagebreak", which is worse than nothing. With it, the same Markdown
gives a real break in Word, a styled div in a web page, \newpage in LaTeX, and
a form feed anywhere else -- so it is safe to write in Markdown intended for
any target.

Copyright (c) 2017-2024 Benct Philip Jonsson, Albert Krewinkel
Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.
]]

local pagebreak = {
  epub  = '<p style="page-break-after: always;"> </p>',
  html  = '<div style="page-break-after: always;"></div>',
  latex = '\\newpage{}',
  ooxml = '<w:p><w:r><w:br w:type="page"/></w:r></w:p>',
  odt   = '<text:p text:style-name="Pagebreak"/>',
}

local function is_pagebreak_command (text)
  local bare = text:gsub('%s', ''):gsub('{}', '')
  return bare == '\\pagebreak' or bare == '\\newpage'
end

local function raw_pagebreak ()
  if FORMAT == 'docx' then
    return pandoc.RawBlock('openxml', pagebreak.ooxml)
  elseif FORMAT == 'odt' then
    return pandoc.RawBlock('opendocument', pagebreak.odt)
  elseif FORMAT:match 'html.*' then
    return pandoc.RawBlock('html', pagebreak.html)
  elseif FORMAT:match 'epub' then
    return pandoc.RawBlock('html', pagebreak.epub)
  elseif FORMAT:match 'latex' or FORMAT:match 'beamer' then
    return pandoc.RawBlock('tex', pagebreak.latex)
  end
  -- Anything else gets a form feed, which is the plain-text convention and
  -- harmless where the idea of a page does not exist.
  return pandoc.Para { pandoc.Str '\012' }
end

function RawBlock (el)
  if el.format:match 'tex' and is_pagebreak_command(el.text) then
    return raw_pagebreak()
  end
end

-- A plain paragraph holding only the command, which is what HomerScribe
-- writes: the Markdown reader does not always see it as raw TeX.
function Para (el)
  if #el.content == 1 and el.content[1].t == 'Str'
     and is_pagebreak_command(el.content[1].text) then
    return raw_pagebreak()
  end
  return nil
end
