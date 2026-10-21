# 2.5DProjekat — Roadmap

Živi dokument razvoja. Faze su redosledom zavisnosti, ne strogim vremenom.
Oznake: **[TI]** = tvoj rad (art/rig/animacije), **[JA]** = moj rad (kod/sistemi/scene), **[ZAJEDNO]** = playtest/odluke.

---

## Faza 0 — Temelji ✅ (završeno)
- [x] Setup: Unity 6000.6, URP, Input System, ProBuilder 6.1.3
- [x] Blockout koridor-levela (~28 × 5 m)
- [x] `PlayerController`: 2.5D kretanje (X + Z koridor), skok sa coyote time i jump buffer
- [x] `CameraFollow2D`: bočni pogled sa nagibom 12°, smooth follow, clamp na ivicama levela

## Faza 1 — Lik (TRENUTNO)
**[TI]**
- [ ] 3D model; proporcije usklađene sa capsule collider-om (visina 2 m, radius 0.5 m) ili uz dogovor o promeni collider-a
- [ ] Rig — **Humanoid** preporučen (retargeting, kasnije animacije iz biblioteka)
- [ ] Animacije **in-place (bez root motion)**: Idle, Walk, Run, JumpStart, JumpLoop/Fall, Land
- [ ] Animation events u klipovima: `FootstepL`, `FootstepR`, `Land`
- [ ] FBX export po konvenciji iz `docs/ANIMACIJE.md` (folder, scale, imena)
**[JA]**
- [ ] `docs/ANIMACIJE.md` — konvencije import-a pre nego što kreneš sa export-om
- [ ] Animator Controller: parametri `Speed`, `VerticalVel`, `Grounded`; blend tree Idle/Walk/Run; state-ovi Jump/Fall/Land
- [ ] `PlayerAnimator` most: PlayerController → Animator parametri; animation events → SFX/efekti
- [ ] Zamena capsule vizuala modelom (collider i controller ostaju autoritativni)

## Faza 2 — Game feel
- [ ] Variable jump height (jump-cut na pustanje dugmeta) — najveći feel-upgrade za najmanje koda
- [ ] Camera look-ahead u smeru kretanja + blagi kick pri punoj brzini
- [ ] Landing: dust particles (URP VFX), SFX, mini camera shake
- [ ] Footstep SFX sinhronizovani animation events-ima
- [ ] `AudioManager`: SFX + muzika, pool-ovanje, glasnoće u Settings

## Faza 3 — Gameplay jezgro (TAČKA ODLUKE O ŽANRU)
- [ ] **[ZAJEDNO]** odluka: borba (combo sistem) ILI čisto platformiranje/puzzles ILI kolekcionarski score
- [ ] Borba (ako da): hitbox/hurtbox po napadu, knockback, hit-stop
- [ ] Platforming (ako da): hazard zone, pokretne platforme, one-way platforme
- [ ] Pickups, checkpoint-i i respawn
- [ ] Health/damage + mini HUD
- [ ] Tok scena: MainMenu → Game → Pause → GameOver

## Faza 4 — Svet: graybox → art
- [ ] **[ZAJEDNO]** beat mapa levela 1: uvod → izazov 1 → eskalacija → vrhunac → izlaz
- [ ] ProBuilder graybox po beat-ovima → playtest → art pass (tvoji mesh-evi/materijali)
- [ ] URP svetlo: baked/mixed GI, post-process tuning, skybox
- [ ] Ambijent zvuk i muzika po zonama

## Faza 5 — Polish i izdanje
- [ ] Juice pass: hit-sparkovi, policy za screenshake, tranzicije, UI polish
- [ ] Performance pass: profiler, budžet tekstura/mesh-eva, build settings
- [ ] PC build + test na slabijoj mašini
- [ ] itch.io / Steam stranica kad bude spremno

---

## Principi rada
1. **Feel pre art-a** — mehanika se doteruje dok je još kocka/kapsula.
2. **Kod je autoritativan** — animacije in-place, kretanje iz controller-a; rig ne vozi fiziku.
3. **Konvencije pre sadržaja** — `docs/` čuva dogovore (imena, folderi, events) da se ništa ne radi dvaput.
4. **Male izmene, čest push** — repo uvek u stanju koje se pokreće.
