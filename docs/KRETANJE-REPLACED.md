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
| `groundFriction` | 16 | skid/decel na pustanje (ne instant stop) |
| `turnSlowdown` | 0.6 | skid pri smeni smera (turn pivot) |
| `hardLandSpeed` / `landRecoveryTime` / `landRecoverySpeedFactor` | 4.5 / 0.12 / 0.55 | kratak recovery posle teskog doskoka |
| `riseGravityMult` | 1.12 | kraca faza uspona (manje float-a) |
| `fallGravityMult` | 1.35 | fast-fall — tezi, brzi pad |
| `dashSpeed` / `dashTime` / `dashCooldown` | 9 / 0.16 / 0.55 | dodge step burst |
| `depthSpeedFactor` | 0.55 | sporija dubina (koridor feel) |

Dodatno: `OnDashed` event (za SFX/VFX dash-a), dash input LeftAlt / gamepad West.

Sve vrednosti su Inspector-tunable — fine-tune po osecaju bez koda.

## Dodato: air rotation lock + cucanj/puzanje

- **Trcanje/hodanje (toggle):** Shift / gamepad L3 — prvi pritisak = trcanje
  (0.8x), drugi = hodanje (0.5x). Stanje ostaje dok se ne pritisne ponovo.
  Trcanje troši staminu (vidi dole).
- **Air rotation lock:** rotacija modela samo na tlu — u vazduhu facing zakljucan
  (realno: ne moze da se okrene u letu).
- **Cucanj (toggle):** Ctrl / gamepad North — collider 1.2 m, brzina 0.6x, rig se
  spusta (noge savijene, telo nagnuto, ruke drze).
- **Puzanje (toggle, prone crawl):** C / gamepad East — collider 0.8 m (radius
  0.4), brzina 0.3x; telo horizontalno (~78°), noge ispruzene, ruke naizmenicno
  vuku. Puzanje i cucanj se medjusobno iskljucuju; ustajanje ide prvo u cucanj pa
  u stajanje (headroom provera za svaku visinu). Skok i dash nisu dozvoljeni u
  cucnju/puzanju.
- **Headroom:** ne ustaje pod preprekom — proverava se mesto za ciljnu visinu.

## Ledge grab (hvatanje ivice + shimmy + mantle)

- **Auto-hvatanje:** dok si u vazduhu (blizu temena skoka/pad) i gledaš u
  vertikalnu ivicu, lik se sam zakači rukama — bez dodatnog tastera.
- **Kada se NE hvata (razlika po visini/udaljenosti):**
  - ivica je niža od `ledgeGrabMinHeight` (0.65 m iznad stopala) — lik na nju
    samo sleti ili se odbije, ne visi;
  - ivica je viša od `ledgeGrabMaxHeight` (2.3 m iznad stopala) — nedohvatna;
  - lik se još diže (`velocity.y > 0.1`) — hvata tek u zenitu/padu, pa ga luk
    skoka normalno odnese na platformu ako je dovoljno niska;
  - lik se kreće od zida (odbija se) — nema hvatanja;
  - stopala bi u visu bila u/ispod poda — nema hvatanja (ne visi kroz pod).
- **Uvek se prvo zakači, nema auto-popeta:** svaka uhvaćena ivica vodi u
  **visenje**; lik se nikad ne popne sam — ni posle skoka, ni na niskim
  ivicama. `autoMantleAfterJump` i `autoMantleLowLedges` su ugašeno po
  defaultu (ostaju samo kao opcioni tuning flagovi u Inspectoru).
- **Visenje:** fizika je privremeno isključena (kinematic), telo visi sa rukama
  na ivici (ruke na ivici, noge slobodno vise, telo okrenuto zidu).
- **Shimmy (pomeranje rukama):** A/D (gamepad left stick X) — pomeranje duž
  ivice; svaki korak proverava da ivica i zid i dalje postoje (ne može da se
  pređe kraj).
- **Penjanje (mantle) tek po izboru:** W (gamepad up) — i to **tek posle novog
  pritiska W** (držanje W kroz sam skok ne penje automatski). Na kontejneru W
  vodi na **vrh kontejnera**
  (`containerClimbTime`), a na ostalim ivicama na ivicu/platformu (~1.05 s).
- **Faze penjanja (proceduralna animacija):** 1) **pull-up** — telo se diže,
  ruke pritišću ivicu (`hangArmRaise` → `hangArmPress`); 2) **kole preko
  ivice** — torzo naginje preko ivice (`mantleBodyLean`), desno koleno se
  zabacuje (`mantleKneeSwing`), levo prati (`mantleKneeOtherSwing`); 3)
  **praví se korak/stajanje** — stopala se prebacuju ispod torza (putanja: rast
  78% vremena, kretanje napred tek poslednjih 50%) i poza se vraća u stojeću.
- **Tok kretanja:** skok → zakači se rukama → visi → pomeranje rukama (A/D)
  ako treba → W se popne na ivicu (ili S pusti).
- **Skok sa ivice:** Space (gamepad South) dok visiš — **ti biraš smer**: drži
  **A/D** pre pritiska za levu/desnu platformu; bez smera skače uvis. Vertikalno
  ~5.5 m/s, horizontalno ~6 m/s u izabranom smeru; posle 0.22 s se okrene u tom
  smeru, a 0.6 s možeš da skreneš u vazduhu. **Radi samo na objektu čije ime
  sadrži `kickOffObjectName` (podrazumevano `Container`)** — na platformama
  Space dok visiš ne odguruje.
- **Puštanje:** S (gamepad down) — blagi odskok od zida i pad.
- **Blokade:** dash, čučanj i puzanje su onemogućeni dok visiš; posle
  puštanja/penjanja postoji kratak cooldown i pauza da zadržani A/D ne odvuče
  lik odmah u stranu.

Tuning (Inspector, header "Ledge grab"): `ledgeGrabReach`, `ledgeGrabMinHeight` /
`ledgeGrabMaxHeight`, `hangFeetOffset`, `ledgeShimmySpeed`, `ledgeJumpVelocity`,
`ledgeJumpPush`, `kickOffObjectName`, `mantleTime`, `containerClimbTime`,
`mantleInset`, `ledgeRegrabDelay`. Animator (header "Ledge hang"):
`hangArmRaise`, `hangArmPress`, `mantleKneeSwing`, `mantleKneeOtherSwing`,
`mantleBodyLean`, `hangLegBend`, `hangBodyLean`, `hangShimmySway`.
Opciono (ugašeno po defaultu):
`autoMantleLowLedges`, `autoMantleMaxHeight`, `quickMantleTime`,
`autoMantleAfterJump`, `autoMantleJumpWindow`.

**Šta je dohvatljivo u trenutnoj sceni** (visina ivice iznad stopala u trenutku
hvatanja mora biti 0.65–2.3 m):

| Objekat | Gornja ivica | Sa zemlje | Sa tanke platforme (~2.6 m) |
|---|---|---|---|
| `Cube (3)` tanke letve | 2.61–2.67 | **da** (u zenitu, delta ~1.4) | – |
| `Obstacle` / `Obstacle (1)` | 0.3 / 0.7 | ne (previše nisko — sleti) | – |
| `Container` / `Container (2)` | 4.91 / 5.10 | ne (delta ~3.7) | **da** (delta ~1.0) |
| `Cube (1)` / `Cube (2)` | 6.65 / 9.67 | ne | ne |

Zid od 4.9 m se **ne može** uhvatiti sa zemlje — do njega se dolazi sa tanke
letve ili preko `jumpOnto` skoka sa niže platforme.

## Skok na ivicu / platformu (Space)

- **Trigger:** Space dok si na zemlji, a ispred je ivica čija je gornja površina
  0.5–2.4 m iznad stopala (`jumpOntoMinHeight` / `jumpOntoMaxHeight`) i na vrhu
  ima mesta za stajanje (`CanStandAt`). Proverava se smer inputa/pogleda i
  uglovi do ±90° (platforma ume da bude uz bok), na više visina (0.3–1.9 m).
- **Animacija:** zalet uvis sa tuck-om u vazduhu, prelazak napred i doskok na
  platformu (proceduralni pose `UpdateJumpOntoPose`, traje `jumpOntoTime`).
- **Putanja:** skriptovani luk — podizanje pa napred, sa malim hop-om
  (`jumpOntoHop`) iznad površine, pa spuštanje tačno na vrh (~0.6 s). Lik ne
  može da promaši/platformu niti da ostane da visi.
- **Ne radi:** ako držiš smer od ivice, ako je plato previsok/nizak, ako na vrhu
  nema mesta ili ako nisi okrenut ka ivici (`jumpOntoMinFacing`).
- **Razlika u odnosu na hvatanje rukama:** niska/platforma na koju možeš da
  skočiš rešava se ovim skokom; visenje o ruke ostaje za ivice koje ne možeš da
  preskočiš (vidi gore).

Tuning (Inspector, header "Skok na ivicu (Space)"): `jumpOntoEnabled`,
`jumpOntoMinHeight`, `jumpOntoMaxHeight`, `jumpOntoReach`, `jumpOntoInset`,
`jumpOntoTime`, `jumpOntoHop`, `jumpOntoMinFacing`.

## Sapletanje preko niske prepreke

- **Trigger:** trcanje (Shift) u nisku prepreku cija je gornja ivica 0.2–0.9 m
  iznad stopala; brzina >= 4 m/s. Hodanje, skok i pad ne triggeruju.
- **Faze:** zaglavi 0.25 s -> pad preko prepreke -> lezi 0.6 s -> ustaje 0.55 s
  (automatski). Trajanje pada se **racuna od udaljenosti**:
  `max(tripFallTime, 1.5 * dist / tripTravelSpeed)` – vrsna brzina je ogranicena
  na `tripTravelSpeed` (3.2 m/s), pa nema "poletanja" napred.
- **Profil visine:** podizanje -> drzanje iznad prepreke -> spust (nema velikog
  luka). Visina je minimalno `visina prepreke + tripClearance`, pa lik prede
  prepreku bez klizanja kroz nju.
- **Pad na drugu stranu:** lik se prebaci preko prepreke, padne i ostane da lezi
  na suprotnoj strani, pa ustane tamo.
- **Tokom:** input je zakljucan, trcanje se prekida, capsule se privremeno
  spusta na crawl visinu; posle ustajanja vraca se stajanje i cooldown 0.7 s.
- Tuning: `tripEnabled`, `tripMinObstacleHeight`, `tripMaxObstacleHeight`,
  `tripMinSpeed`, `tripStuckTime`, `tripFallTime`, `tripLieTime`,
  `tripGetUpTime`, `tripCooldown`, `tripHopHeight`, `tripLandingMargin`,
  `tripTravelSpeed`, `tripClearance`.
- Test: `Obstacle` na x ≈ -4.25 (niska prepreka na tlu).

## Prekoracenje pri hodanju (vault)

- **Trigger:** hodanje (Shift iskljucen) u nisku prepreku 0.2–0.9 m;
  brzina 1.2–4.5 m/s.
- **Cilj prelaska** se racuna od **daleke ivice prepreke** u pravcu kretanja
  (ray vs AABB), ne od centra prepreke – inace je dijagonalni prilaz davao
  ogroman cilj i lik je "poletao" napred. Ako bi prelaz bio duzi od
  `vaultMaxTravel` (2.6 m), prelaza nema.
- **Male prepreke (<= 0.65 m):** suptilni step-over; kratak zakorak jednom nogom,
  blagi nagib. Faze: 0.08 s -> prelaz -> 0.1 s.
- **Vece niske prepreke (0.65-0.9 m):** izrazeniji vault; oslonac rukom 0.15 s ->
  prelaz -> doskok 0.18 s.
- **Brzina je ogranicena:** vreme prelaza se racuna kao
  `max(bazno, 1.5 * dist / travelSpeed)`, pa je vrsna brzina step-over-a
  4.2 m/s, a vault-a 5 m/s (hod je 3.3 m/s) – nema "poletanja".
- **Profil visine:** podizanje -> drzanje iznad prepreke -> spust; minimalna
  visina je `visina prepreke + vaultClearance`, pa lik ne prolazi kroz prepreku
  i ne leti visoko iznad nje.
- U oba slucaja lik prelazi na drugu stranu bez pada, a momentum se nosi dalje
  (0.6x od brzine na ulazu) umesto da se naglo zaustavi.
- **Ako je druga strana blokirana ili nema tla:** nema prelaska, lik se zaustavi.
- **Space ima prioritet:** ako je skok pritisnut, auto-prelazak se ne pokrece;
  ako je vec poceo u fazi oslonca, otkazuje se i izvodi se skok; u fazama prelaza
  i doskoka skok se pamti i izvodi odmah posle prelaza.
- **Trcanje:** i dalje sapletanje/pad (trip).
- Tuning: `vaultEnabled`, `vaultMinSpeed`, `vaultMaxSpeed`, `vaultPlantTime`,
  `vaultOverTime`, `vaultLandTime`, `vaultHopHeight`, `vaultLandingMargin`,
  `vaultCooldown`, `vaultTravelSpeed`, `vaultClearance`, `vaultMaxTravel`,
  `stepOverMaxHeight`, `stepOverPlantTime`, `stepOverOverTime`,
  `stepOverLandTime`, `stepOverHopHeight`, `stepOverLandingMargin`,
  `stepOverTravelSpeed`.

## Stamina

- **Max:** 100; trcanje troši **30/s** (~3.3 s neprekidnog trcanja).
- **Regeneracija:** **15/s** posle **0.7 s** bez trcanja (i dok stojiš/miruješ).
- **Iscrpljenost:** na 0 stamina lik je prisilno na hod; Shift se može ponovo
  uključiti tek kad se skupi **15** stamine.
- **HUD:** mali bar gore levo (`showStaminaHud` u PlayerController-u; skripta
  `StaminaHud.cs` se dodaje automatski).
- Tuning: `maxStamina`, `staminaDrainPerSecond`, `staminaRegenPerSecond`,
  `staminaRegenDelay`, `staminaMinToStart`, `runSpeedFactor`.

Test geometrija: postojeći ProBuilder Cube.
