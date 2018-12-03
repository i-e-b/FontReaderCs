# FontReaderCs
A dead simple font reader and renderer, for experimentation 

Font "Dave.ttf" is by me, and is free to use.

Font "Guthen Bloots" is by Azetype86 and is free for personal use. For full commercial version, see ( https://crmrkt.com/d1b47b )

Font "NotoSans-Regular" is by Google, and is licensed under the SIL Open Font License.

## Plans

* [x] Simple scanline renderer
* [x] Anti-aliasing and sub-pixel rendering
* [x] Try breaking curves in the font reader
* [x] Dark-on-light rendering (or better, proper coloring and alpha?)
* [x] Basic auto hinting? (grid fit + jitter control)
* [ ] Better sizing interface (rather than scale)
      - Font*Size container to cache contour normalisation
* [ ] Try a simple "directional signed distance field" renderer (scan lines in horizonal and vertical separately?)