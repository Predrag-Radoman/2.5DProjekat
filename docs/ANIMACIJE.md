# Konvencije za model, rig i animacije Player-a

Cilj: da tvoj export iz Blender-a (ili drugog alata) legne u Unity **bez ijednog ponovnog rada**.
Sve što je ovde dogovoreno već je pripremljeno u kodu i Animator Controller-u.

## 1. Folderi i imena fajlova
```
Assets/Art/Characters/Player/
    Player_Model.fbx          <- mesh + rig (bez animacija)
    Player_Anim_Idle.fbx      <- jedan FBX po animaciji
    Player_Anim_Walk.fbx
    Player_Anim_Run.fbx
    Player_Anim_JumpStart.fbx
    Player_Anim_JumpLoop.fbx
    Player_Anim_Land.fbx      <- optional za sada (state u Animator-u se dodaje kad stigne)
    Materials/ , Textures/    <- ako ih ima
```

## 2. Model i rig
- **Mere:** visina lika ~1.8–2.0 m (capsule collider je trenutno 2 m — ako model bude niži, reci mi da srazmerim collider).
- **Pivot:** origin objekta na **stopalima** (tlo = Y0), ne u centru tela.
- **Rig:** **Humanoid** (Unity Mecanim humanoid avatar) — zbog retarget-a i biblioteka animacija kasnije.
- **Forward smer:** lice lika gleda u **+Z** nakon import-a. U Blender-u: lik gleda u **-Y**, a FBX export sa **default axis settings (Forward: -Z, Up: Y)** daje ispravan rezultat u Unity-ju.
- **Scale:** scene units u Blender-u = metri (1.0), FBX export **Apply Scale: FBX All / Unit scale 1.0** — u Unity-ju Transform scale mora biti (1,1,1).
- Apply-uj sve transform-e pre export-a (rotation/scale na objektu = identity).

## 3. Animacije — lista i pravila
| Klip (tačno ime) | Tip | Loop | Trajanje (okvirno) |
|---|---|---|---|
| `Idle` | in-place | **DA** | 2–4 s |
| `Walk` | in-place | **DA** | 1 ciklus |
| `Run` | in-place | **DA** | 1 ciklus |
| `JumpStart` | in-place | NE | 0.15–0.3 s |
| `JumpLoop` | in-place | NE (ili DA, kratko) | 0.3–0.6 s (fall poza) |
| `Land` | in-place | NE | 0.2–0.4 s |

- **In-place = bez root motion.** Kretanje i skok su 100% iz koda (`PlayerController`); animacije ne pomeraju root. Root bone/objekat miruje u XZ, i u Y osim ako poenta poze nije vizuelna (npr. blago čučanje).
- Frame rate konstantan (30 ili 60 fps), bez key-eva na root objektu.
- **Animation events** (dodaj u Blender-u ili ih dodajem ja u Unity import settings):
  - `FootstepL` / `FootstepR` — na frejmu kontakta stopala u Walk i Run
  - `Land` event **nije obavezan** — kod već detektuje doskok (`OnLanded`), ali ako ga dodaš, koristi se za SFX preciznije.

## 4. Animator parametri (VEĆ postoje u `PlayerAnimatorController`)
| Parametar | Tip | Značenje |
|---|---|---|
| `Speed` | float 0–1 | normalizovana horizontalna brzina (0 = stoji, 1 = pun trk) |
| `VerticalVel` | float m/s | Y brzina (5.5 na skoku, 0 na vrhu, negativno u padu) |
| `Grounded` | bool | da li je na tlu |
| `Jump` | trigger | okida se u trenutku skoka |

Postojeći state-ovi: **Locomotion** (blend tree Idle→Walk→Run po `Speed`, pragovi 0 / 0.5 / 1), **JumpStart**, **JumpLoop**.
Kad stignu klipovi: samo se m_Motion slotovi popune (ja to uradim), prelazi ostaju.
`Land` state + footstep-driven blend (npr. walk/run nagib po brzini) doterujemo u fazi polish-a.

## 5. Check-lista pre push-a
- [ ] Transform scale (1,1,1), pivot na stopalima
- [ ] Humanoid rig konfigurisan (Avatar validan, zelena kvačka u Rig tabu)
- [ ] Imena klipova TAČNO kao u tabeli (case-sensitive)
- [ ] Loop flag: Idle/Walk/Run = loop, ostali ne
- [ ] Nema root motion key-eva
- [ ] FBX-ovi u `Assets/Art/Characters/Player/`, pa `git add` + push
