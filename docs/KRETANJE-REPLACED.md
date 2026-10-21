# REPLACED feel — analiza kretanja i kako je oponasano

**REPLACED** (Sad Cat Studios) — 2.5D akcioni platformer (alternativne 1980-te):
fluidan, "težak ali responsivan" movement sa momentum-om i akrobatskim detaljima.

## Sta ga cini prepoznatljivim (iz design analiza/preview-a)

1. **Momentum run** — ubrzanje sa ease-out krivom blizu top speed-a (ne linearno);
   na pustanje/smenu smera **skid/decel** (ne instant stop) → tezina + responsivnost.
2. **Težak ali brz jump** — variable height + coyote + buffer; **fast-fall**:
   jaca gravitacija u padu nego u usponu (brzi, tezi doskok; uspon blago floaty).
3. **Turn pivot sa skid-om** — pri smeni smera kratko usporavanje pre pivota.
4. **Dodge step / dash** — kratki burst-impuls sa cooldown-om (Shift / gamepad West).
5. **Sporiji depth (Z) movement** — koridor feel: dubina ~0.55 brzine bocnog kretanja.
6. **Smanjen air control** — manji air authority + fast-fall kombinacija.

## Kako je oponasano u PlayerController-u (header "REPLACED feel")

| Parametar | Vrednost | REPLACED osobina |
|---|---|---|
| `runEaseNearTop` | 0.55 | ease-out ubrzanje blizu top speed-a (momentum run) |
| `groundFriction` | 14 | skid/decel na pustanje (ne instant stop) |
| `turnSlowdown` | 0.6 | skid pri smeni smera (turn pivot) |
| `riseGravityMult` | 0.95 | blago floaty uspon |
| `fallGravityMult` | 1.35 | fast-fall — tezi, brzi pad |
| `dashSpeed` / `dashTime` / `dashCooldown` | 9 / 0.16 / 0.55 | dodge step burst |
| `depthSpeedFactor` | 0.55 | sporija dubina (koridor feel) |

Dodatno: `OnDashed` event (za SFX/VFX dash-a), dash input LeftShift / gamepad West.

Sve vrednosti su Inspector-tunable — fine-tune po osecaju bez koda.

## Dodato: air rotation lock + cucanj/puzanje

- **Air rotation lock:** rotacija modela samo na tlu — u vazduhu facing zakljucan
  (realno: ne moze da se okrene u letu).
- **Cucanj:** Ctrl / C / gamepad North — rig se spusta (hips drop, noge savijene,
  telo nagnuto, ruke drze), collider visina se smanjuje (headroom provera:
  ne ustaje pod preprekom). Skok nije dozvoljen u cucnju.
- **Puzanje:** kretanje u cucnju = 0.4x brzina, sporiji i nizi walk cycle
  (crouchSwingScale) — stealth/koridor feel.
