import React from "react";
import { interpolate } from "remotion";
import { C, FONT, ICON_FONT } from "./theme";
import { clamp } from "./util";

export interface ChatMessage {
  from: "them" | "me";
  text: string;
  at: number;
}

const Avatar: React.FC<{ letter: string; color: string; size: number }> = ({ letter, color, size }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: size / 2,
      background: color,
      color: "#fff",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontSize: size * 0.46,
      fontWeight: 600,
      flexShrink: 0,
    }}
  >
    {letter}
  </div>
);

const CONTACTS = [
  { name: "Lisa", color: "#c2185b", preview: "Where should we go?" },
  { name: "Team", color: "#00897b", preview: "Standup moved to 10:15" },
  { name: "Mom", color: "#f57c00", preview: "Call me later" },
];

// A generic dark chat page in a browser window; the dictated text lands in its message field.
export const ChatApp: React.FC<{ f: number; messages: ChatMessage[]; draft: string; contact?: string }> = ({ f, messages, draft, contact = "Lisa" }) => {
  const caretOn = Math.floor(f * 2) % 2 === 0 || draft.length > 0;
  const active = CONTACTS.find((c) => c.name === contact) ?? CONTACTS[0];
  return (
    <div
      style={{
        width: 1500,
        height: 740,
        background: "#1e1f22",
        borderRadius: 14,
        border: "1px solid #3c4043",
        boxShadow: "0 40px 100px rgba(0,0,0,0.6)",
        fontFamily: FONT,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <div style={{ height: 52, background: "#17181a", display: "flex", alignItems: "flex-end", paddingLeft: 16 }}>
        <div
          style={{
            height: 40,
            width: 300,
            background: "#2b2c2f",
            borderRadius: "10px 10px 0 0",
            display: "flex",
            alignItems: "center",
            padding: "0 16px",
            color: C.text,
            fontSize: 18,
            gap: 10,
          }}
        >
          <div style={{ width: 16, height: 16, borderRadius: 4, background: C.link }} />
          Chat · {active.name}
        </div>
      </div>
      <div style={{ height: 56, background: "#2b2c2f", display: "flex", alignItems: "center", padding: "0 20px", gap: 16 }}>
        <span style={{ fontFamily: ICON_FONT, color: C.muted, fontSize: 18 }}>{""}</span>
        <span style={{ fontFamily: ICON_FONT, color: C.muted, fontSize: 18 }}>{""}</span>
        <div
          style={{
            flex: 1,
            height: 36,
            borderRadius: 18,
            background: "#17181a",
            display: "flex",
            alignItems: "center",
            padding: "0 18px",
            color: C.text,
            fontSize: 18,
            gap: 10,
          }}
        >
          <span style={{ fontFamily: ICON_FONT, fontSize: 14, color: C.level }}>{""}</span>
          chat.example.com/{active.name.toLowerCase()}
        </div>
      </div>
      <div style={{ flex: 1, display: "flex", minHeight: 0 }}>
        <div style={{ width: 300, background: "#17181a", padding: "18px 0" }}>
          <div style={{ color: C.muted, fontSize: 16, padding: "0 22px 12px", letterSpacing: 1 }}>CHATS</div>
          {CONTACTS.map((c) => (
            <div
              key={c.name}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 14,
                padding: "12px 22px",
                background: c.name === active.name ? "#2b2c2f" : "transparent",
              }}
            >
              <Avatar letter={c.name[0]} color={c.color} size={46} />
              <div style={{ minWidth: 0 }}>
                <div style={{ color: C.text, fontSize: 20 }}>{c.name}</div>
                <div style={{ color: C.muted, fontSize: 15, whiteSpace: "nowrap" }}>{c.preview}</div>
              </div>
            </div>
          ))}
        </div>
        <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0 }}>
          <div style={{ height: 70, display: "flex", alignItems: "center", gap: 14, padding: "0 28px", borderBottom: "1px solid #2b2c2f" }}>
            <Avatar letter={active.name[0]} color={active.color} size={42} />
            <div style={{ color: C.text, fontSize: 22, fontWeight: 600 }}>{active.name}</div>
            <div style={{ color: C.level, fontSize: 15 }}>online</div>
          </div>
          <div style={{ flex: 1, display: "flex", flexDirection: "column", justifyContent: "flex-end", gap: 14, padding: "0 28px 18px" }}>
            {messages
              .filter((m) => f >= m.at)
              .map((m, i) => (
                <div
                  key={i}
                  style={{
                    alignSelf: m.from === "me" ? "flex-end" : "flex-start",
                    maxWidth: 820,
                    padding: "12px 22px",
                    borderRadius: 20,
                    background: m.from === "me" ? "#2b5bb5" : "#2f3136",
                    color: "#fff",
                    fontSize: 26,
                    lineHeight: "36px",
                    opacity: interpolate(f, [m.at, m.at + 0.35], [0, 1], clamp),
                  }}
                >
                  {m.text}
                </div>
              ))}
          </div>
          <div style={{ height: 96, display: "flex", alignItems: "center", gap: 16, padding: "0 28px" }}>
            <div
              style={{
                flex: 1,
                minHeight: 60,
                borderRadius: 30,
                background: "#2b2c2f",
                border: `2px solid ${C.link}`,
                display: "flex",
                alignItems: "center",
                padding: "0 24px",
                fontSize: 26,
                color: C.text,
                overflow: "hidden",
                whiteSpace: "nowrap",
              }}
            >
              {draft.length === 0 && <span style={{ borderLeft: `3px solid ${caretOn ? C.text : "transparent"}`, marginRight: -3 }}>&#8203;</span>}
              {draft.length === 0 ? <span style={{ color: "#6f7378" }}>Type a message…</span> : draft}
              {draft.length > 0 && caretOn && <span style={{ borderLeft: `3px solid ${C.text}`, marginLeft: 2 }}>&#8203;</span>}
            </div>
            <div
              style={{
                width: 60,
                height: 60,
                borderRadius: 30,
                background: "#2b5bb5",
                color: "#fff",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontFamily: ICON_FONT,
                fontSize: 24,
              }}
            >
              {""}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
