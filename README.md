# 🧿 Cryptid VR - Multiplayer Cryptid Survival Experience

Welcome to **Cryptid VR**, a stylized, fast-paced multiplayer VR experience built with Unity and Photon Fusion. Players take on the role of legendary cryptids — Bigfoot, Mothman, Frogman, or Alien — in a series of uniquely themed game modes that blend parkour locomotion, competitive survival, and investigative stealth.

---

## 🎮 Game Overview

**Cryptid VR** is a multiplayer game inspired by **Gorilla Tag**, but with distinct game modes, character abilities, and asymmetric objectives across three core PvP and social modes.

Built with:
- **Unity 2022+**
- **Photon Fusion (Client-Server mode)**
- **XR Interaction Toolkit**
- **GorillaLocomotion**
- **Custom modular player rig architecture**

---

## 🧬 Game Modes

### 🧟‍♂️ Tag Mode (Infection Style)
- Classic Gorilla Tag-inspired mode.
- Once enough players join or a timer ends, a random player becomes **Rabid**.
- Rabid cryptids can infect others by tagging them.
- The last player to be infected is declared the winner.
- Rabid players gain unique MeshRenderer materials and nameplate indicators.

### 🔫 Battle Mode (Free-for-All Elimination)
- Each cryptid starts with **3 lives** and a **tranquilizer gun**.
- Getting hit stuns and removes a life.
- Eliminated players continue playing with **non-lethal darts** that **slow others**.
- **Last of Their Kind**: Last living member of a cryptid species gets a movement/jump buff.
- Last cryptid alive wins the match.

### 🕵️‍♂️ Decryptid Mode (Hunt-and-Expose)
- Each player is assigned a **random target** to photograph.
- Use a special camera to snap photos and catch them in the act.
- Successfully capturing a target may rotate your objective to a new player.
- Highlights or detection logic assist in identifying if your target is in-frame.
- Asymmetric stealth and observation mechanic.

### 🧘 Free Play Mode (Casual Sandbox)
- Non-competitive sandbox for practicing movement, hanging out, or exploring.
- Includes an in-world button to return to the lobby at any time.

---

## 🧠 Gameplay Features

- **Cryptid Abilities**: Each cryptid has its own locomotion and strengths. (WIP)
- **Status Effects**: Stunned, Invulnerable, Slowed, Buffed, and Eliminated — all tracked with icons.
- **Nameplate UI**: Floating player names with status-based color highlights.
- **Voice Chat (planned)**: Integrated Photon Voice support.
- **Match Timer / Countdown UI**: Fade canvas handles match start/end and screen text.
- **Dynamic spawn system**: Assigns spawn points per mode.
- **Desktop support** for testing without a headset.

---

## 🔁 Networking Architecture

- Built on **Photon Fusion in Client-Server mode**
- **Dedicated NetworkRunnerHandler** handles connection lifecycle, spawning, and lobby transitions.
- All player actions network-authoritative; includes **NetworkRigidbody3D** and **NetworkTransform**.
- **Dynamic prefab spawning** based on player-selected cryptid.
- **Match-specific managers** (e.g. TagModeManager, BattleModeManager) handle per-mode logic and tracking.

---

## 🧪 Development Notes

- Compatible with **ParrelSync** for local multiplayer testing.
- Desktop prefab is used automatically for clone projects.
- All Networked lists (e.g. ActivePlayers, RabidPlayers) use **NetworkLinkedList**.
- Decryptid mode uses **camera-based detection** (with optional highlight system planned).
- Project uses `DontDestroyOnLoad` singleton managers for persistent logic and connection handling.

---

## 🔧 Future Features (WIP)

- Cryptid-specific abilities (e.g., Mothman glide, Alien dash).
- Scorekeeping and progression per match.
- Voice chat via Photon Voice integration.
- Spectator mode and post-game summaries.
- Match rematch system / matchmaking UI.
- In-game cosmetics and customization options.

---

## 🛠️ Setup Instructions

1. Clone this repo:  
   `git clone https://github.com/yourusername/cryptid-vr.git`

2. Open in **Unity 2022.3+** with XR Plugin Management and URP installed.

3. Create a **Photon Fusion App** and add your App ID to the Fusion settings.

4. Setup ParrelSync (optional) for local testing.

5. Press Play in the Lobby scene and start your cryptid career.

---

## 🧞‍♂️ Credits

- Developed by: **Joshua Williams**
- GorillaLocomotion base by: Another Axiom
- Multiplayer via: Photon Fusion
- XR Framework: Unity XR Toolkit

---

## 📸 License

MIT License — feel free to fork, modify, and haunt the woods.


