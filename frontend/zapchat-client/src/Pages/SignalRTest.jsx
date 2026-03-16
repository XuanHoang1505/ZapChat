import { useState, useRef, useEffect } from "react";
import * as signalR from "@microsoft/signalr";

const API = "http://localhost:5003";
const HUB = "http://localhost:5003/hubs/chat";

export default function SignalRTest() {
  const [token, setToken] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [connected, setConnected] = useState(false);
  const [logs, setLogs] = useState([]);
  const [convId, setConvId] = useState("");
  const [msgContent, setMsgContent] = useState("");
  const [msgId, setMsgId] = useState("");
  const [emoji, setEmoji] = useState("❤️");
  const [negotiateInfo, setNegotiateInfo] = useState(null);

  const connRef = useRef(null);
  const logRef = useRef(null);

  useEffect(() => {
    if (logRef.current) logRef.current.scrollTop = logRef.current.scrollHeight;
  }, [logs]);

  const log = (msg, type = "sys") => {
    const time = new Date().toLocaleTimeString("vi-VN");
    setLogs((prev) => [...prev, { time, msg, type }]);
    console.log(`[${type.toUpperCase()}] ${msg}`);
  };

  const logColor = {
    in: "#4ade80",
    out: "#60a5fa",
    err: "#f87171",
    sys: "#fbbf24",
    dbg: "#c084fc",
  };

  // ── Login ─────────────────────────────────────────
  const handleLogin = async () => {
    log(`🔐 Đang login: ${email}`, "dbg");
    try {
      const res = await fetch(`${API}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      log(`📡 Login response status: ${res.status}`, "dbg");
      const data = await res.json();
      log(`📡 Login response: ${JSON.stringify(data).slice(0, 200)}`, "dbg");

      if (!data.success) {
        log(`❌ Login thất bại: ${data.message}`, "err");
        return;
      }

      const t = data.data.accessToken;
      setToken(t);
      log(`✅ Login thành công!`, "sys");
      log(`🔑 Token (60 ký tự đầu): ${t.slice(0, 60)}...`, "dbg");
    } catch (err) {
      log(`❌ Login exception: ${err.message}`, "err");
      console.error(err);
    }
  };

  // ── Test Negotiate ─────────────────────────────────
  const testNegotiate = async () => {
    log(`🔍 Test negotiate endpoint...`, "dbg");
    try {
      const res = await fetch(`${API}/hubs/chat/negotiate?negotiateVersion=1`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      });

      log(`📡 Negotiate status: ${res.status}`, "dbg");
      const text = await res.text();
      log(`📡 Negotiate response: ${text.slice(0, 300)}`, "dbg");
      setNegotiateInfo(`Status: ${res.status} | ${text.slice(0, 200)}`);

      if (res.status === 200) log(`✅ Negotiate OK!`, "sys");
      else if (res.status === 401)
        log(`❌ Negotiate 401 — Token không hợp lệ hoặc thiếu!`, "err");
      else if (res.status === 403) log(`❌ Negotiate 403 — Forbidden!`, "err");
      else if (res.status === 404)
        log(
          `❌ Negotiate 404 — Hub không tồn tại! Kiểm tra MapHub<ChatHub>()`,
          "err",
        );
      else log(`⚠️ Negotiate status lạ: ${res.status}`, "err");
    } catch (err) {
      log(`❌ Negotiate exception: ${err.message}`, "err");
      log(`💡 Có thể backend chưa chạy hoặc sai port`, "err");
      console.error(err);
    }
  };

  // ── Test CORS ──────────────────────────────────────
  const testCors = async () => {
    log(`🔍 Test CORS...`, "dbg");
    try {
      const res = await fetch(`${API}/api/auth/me`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      log(`📡 CORS test status: ${res.status}`, "dbg");
      if (res.ok) log(`✅ CORS OK!`, "sys");
      else log(`⚠️ CORS response: ${res.status}`, "err");
    } catch (err) {
      log(`❌ CORS blocked: ${err.message}`, "err");
      log(`💡 Kiểm tra AllowedOrigins trong appsettings`, "err");
    }
  };

  // ── Connect ────────────────────────────────────────
  const handleConnect = async () => {
    if (!token) {
      log("❌ Chưa có token! Hãy login trước.", "err");
      return;
    }

    log(`🔌 Đang kết nối tới: ${HUB}`, "dbg");
    log(`🔑 Dùng token: ${token.slice(0, 40)}...`, "dbg");

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB, {
        accessTokenFactory: () => {
          log(`🔑 accessTokenFactory được gọi`, "dbg");
          return token;
        },
        // Thử tất cả transport
        transport:
          signalR.HttpTransportType.WebSockets |
          signalR.HttpTransportType.ServerSentEvents |
          signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Debug)
      .build();

    // ── Events ────────────────────────────────────────
    conn.on("ReceiveMessage", (msg) =>
      log(
        `📨 ReceiveMessage | conv:${msg.conversationId} | ${msg.senderName}: ${msg.content ?? "[file]"} | id:${msg.id}`,
        "in",
      ),
    );

    conn.on("UserTyping", (d) =>
      log(`⌨️  UserTyping | conv:${d.conversationId} | ${d.displayName}`, "in"),
    );

    conn.on("UserStopTyping", (d) =>
      log(`⌨️  UserStopTyping | conv:${d.conversationId}`, "in"),
    );

    conn.on("MessageRead", (d) =>
      log(`✅ MessageRead | msg:${d.messageId} | user:${d.userId}`, "in"),
    );

    conn.on("MessageReacted", (d) =>
      log(`😀 MessageReacted | msg:${d.messageId} | ${d.reaction.emoji}`, "in"),
    );

    conn.on("ReactionRemoved", (d) =>
      log(`❌ ReactionRemoved | msg:${d.messageId} | ${d.emoji}`, "in"),
    );

    conn.on("MessageEdited", (msg) =>
      log(`✏️  MessageEdited | id:${msg.id} | "${msg.content}"`, "in"),
    );

    conn.on("MessageDeleted", (d) =>
      log(`🗑️  MessageDeleted | id:${d.messageId}`, "in"),
    );

    conn.on("UserOnline", (id) => log(`🟢 UserOnline  | ${id}`, "sys"));
    conn.on("UserOffline", (id) => log(`🔴 UserOffline | ${id}`, "sys"));

    conn.on("ConversationCreated", (c) =>
      log(`💬 ConversationCreated | id:${c.id}`, "sys"),
    );

    conn.on("FriendRequestReceived", (r) =>
      log(`👥 FriendRequest | from:${r.displayName}`, "sys"),
    );

    conn.onreconnecting((err) => {
      setConnected(false);
      log(`🔄 Reconnecting... ${err?.message ?? ""}`, "sys");
    });

    conn.onreconnected((connId) => {
      setConnected(true);
      log(`✅ Reconnected! connId: ${connId}`, "sys");
    });

    conn.onclose((err) => {
      setConnected(false);
      log(`🔌 Connection closed. ${err?.message ?? ""}`, "sys");
      if (err) log(`❌ Close error: ${JSON.stringify(err)}`, "err");
    });

    try {
      log(`⏳ Đang gọi connection.start()...`, "dbg");
      await conn.start();
      connRef.current = conn;
      setConnected(true);
      log(`✅ Kết nối thành công!`, "sys");
      log(`🆔 connectionId: ${conn.connectionId}`, "dbg");
      log(`🚦 State: ${conn.state}`, "dbg");
    } catch (err) {
      log(`❌ Kết nối thất bại!`, "err");
      log(`❌ Error message: ${err.message}`, "err");
      log(`❌ Error stack: ${err.stack?.slice(0, 300)}`, "err");
      log(
        `❌ Full error: ${JSON.stringify(err, Object.getOwnPropertyNames(err))}`,
        "err",
      );
      console.error("SignalR connect error:", err);

      // Gợi ý fix
      if (err.message?.includes("401"))
        log(`💡 Fix: Token hết hạn hoặc Jwt:Key sai → login lại`, "err");
      else if (err.message?.includes("404"))
        log(
          `💡 Fix: Hub không tồn tại → kiểm tra app.MapHub<ChatHub>("/hubs/chat")`,
          "err",
        );
      else if (
        err.message?.includes("Failed to fetch") ||
        err.message?.includes("CORS")
      )
        log(
          `💡 Fix: CORS blocked → kiểm tra AllowedOrigins trong appsettings`,
          "err",
        );
      else if (err.message?.includes("503"))
        log(`💡 Fix: Backend chưa chạy hoặc sai port`, "err");
    }
  };

  const handleDisconnect = async () => {
    if (connRef.current) {
      log(`🔌 Đang ngắt kết nối...`, "dbg");
      await connRef.current.stop();
      connRef.current = null;
      setConnected(false);
      log(`✅ Đã ngắt kết nối.`, "sys");
    }
  };

  // ── Invoke ─────────────────────────────────────────
  const invoke = async (method, ...args) => {
    if (!connRef.current || !connected) {
      log("❌ Chưa kết nối SignalR!", "err");
      return;
    }
    try {
      // Log data trước khi gửi
      log(`📤 Invoke: ${method} | data: ${JSON.stringify(args)}`, "out");
      await connRef.current.invoke(method, ...args);
      log(`✅ ${method} thành công`, "out");
    } catch (err) {
      log(`❌ ${method} error: ${err.message}`, "err");
      console.error(`${method} error:`, err);
    }
  };

  const sendMessage = () => {
    if (!convId || !msgContent.trim()) {
      log("❌ Nhập conversationId và nội dung!", "err");
      return;
    }
    invoke("SendMessage", {
      conversationId: parseInt(convId),
      content: msgContent.trim(),
      type: "Text",
      replyToId: null,
      attachments: [],
    });
    setMsgContent("");
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  // ── UI ─────────────────────────────────────────────
  return (
    <div
      style={{
        fontFamily: "Arial",
        background: "#1a1a2e",
        minHeight: "100vh",
        color: "#eee",
        padding: 20,
      }}
    >
      <h2 style={{ color: "#e94560", marginBottom: 4 }}>
        ⚡ ZapChat — SignalR Debug
      </h2>
      <div style={{ fontSize: 12, color: "#555", marginBottom: 16 }}>
        API: {API} | HUB: {HUB}
      </div>

      <div
        style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 12 }}
      >
        {/* Login */}
        <Card title="🔐 LOGIN">
          <Input placeholder="Email" value={email} onChange={setEmail} />
          <Input
            placeholder="Password"
            type="password"
            value={password}
            onChange={setPassword}
          />
          <Btn onClick={handleLogin}>Login</Btn>
          {token && (
            <div
              style={{
                fontSize: 10,
                color: "#4ade80",
                marginTop: 6,
                wordBreak: "break-all",
                lineHeight: 1.4,
              }}
            >
              ✅ {token.slice(0, 50)}...
            </div>
          )}
        </Card>

        {/* Debug */}
        <Card title="🔍 DEBUG">
          <div style={{ fontSize: 11, color: "#888", marginBottom: 8 }}>
            Kiểm tra từng bước trước khi connect
          </div>
          <Btn onClick={testCors} style={{ background: "#0f3460" }}>
            Test CORS
          </Btn>
          <Btn onClick={testNegotiate} style={{ background: "#0f3460" }}>
            Test Negotiate
          </Btn>
          {negotiateInfo && (
            <div
              style={{
                fontSize: 10,
                color: "#c084fc",
                marginTop: 8,
                wordBreak: "break-all",
                lineHeight: 1.4,
              }}
            >
              {negotiateInfo}
            </div>
          )}
        </Card>

        {/* Kết nối */}
        <Card title="🔌 KẾT NỐI">
          <div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 8,
              marginBottom: 12,
            }}
          >
            <span
              style={{
                width: 12,
                height: 12,
                borderRadius: "50%",
                background: connected ? "#4ade80" : "#f87171",
                boxShadow: connected ? "0 0 8px #4ade80" : "none",
              }}
            />
            <span style={{ fontSize: 13 }}>
              {connected ? "✅ Đã kết nối" : "🔴 Chưa kết nối"}
            </span>
          </div>
          <Btn onClick={handleConnect} disabled={connected}>
            Kết nối
          </Btn>
          <Btn
            onClick={handleDisconnect}
            disabled={!connected}
            style={{ background: "#555" }}
          >
            Ngắt kết nối
          </Btn>
        </Card>

        {/* Gửi tin nhắn */}
        <Card title="💬 GỬI TIN NHẮN">
          <Input
            placeholder="conversationId"
            type="number"
            value={convId}
            onChange={setConvId}
          />
          <textarea
            placeholder="Nội dung (Enter để gửi)"
            value={msgContent}
            onChange={(e) => setMsgContent(e.target.value)}
            onKeyDown={handleKeyDown}
            rows={3}
            style={inputStyle}
          />
          <Btn onClick={sendMessage}>Gửi</Btn>
        </Card>

        {/* Typing */}
        <Card title="⌨️ TYPING">
          <Input
            placeholder="conversationId"
            type="number"
            value={convId}
            onChange={setConvId}
          />
          <Btn onClick={() => invoke("TypingStart", parseInt(convId))}>
            Bắt đầu gõ
          </Btn>
          <Btn
            onClick={() => invoke("TypingStop", parseInt(convId))}
            style={{ background: "#555" }}
          >
            Ngừng gõ
          </Btn>
        </Card>

        {/* Read + Reaction */}
        <Card title="✅ READ / 😀 REACT">
          <Input
            placeholder="messageId"
            type="number"
            value={msgId}
            onChange={setMsgId}
          />
          <Btn onClick={() => invoke("MarkAsRead", parseInt(msgId))}>
            Mark Read
          </Btn>
          <hr style={{ border: "1px solid #0f3460", margin: "8px 0" }} />
          <Input placeholder="Emoji" value={emoji} onChange={setEmoji} />
          <Btn onClick={() => invoke("ReactToMessage", parseInt(msgId), emoji)}>
            React
          </Btn>
          <Btn
            onClick={() => invoke("RemoveReaction", parseInt(msgId), emoji)}
            style={{ background: "#555" }}
          >
            Bỏ React
          </Btn>
        </Card>
      </div>

      {/* Log */}
      <div
        style={{
          background: "#16213e",
          borderRadius: 8,
          padding: 16,
          marginTop: 12,
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: 8,
          }}
        >
          <span
            style={{
              background: "#e94560",
              color: "white",
              padding: "4px 10px",
              borderRadius: 4,
              fontSize: 12,
              fontWeight: "bold",
            }}
          >
            📋 LOG ({logs.length})
          </span>
          <div>
            <button
              onClick={() => setLogs([])}
              style={{ ...btnStyle, background: "#555", fontSize: 11 }}
            >
              Xoá log
            </button>
            <button
              onClick={() => {
                const text = logs.map((l) => `[${l.time}] ${l.msg}`).join("\n");
                navigator.clipboard.writeText(text);
                log("📋 Đã copy log!", "sys");
              }}
              style={{ ...btnStyle, background: "#0f3460", fontSize: 11 }}
            >
              Copy log
            </button>
          </div>
        </div>

        {/* Legend */}
        <div
          style={{ display: "flex", gap: 16, marginBottom: 8, fontSize: 11 }}
        >
          {Object.entries({
            in: "← Nhận",
            out: "→ Gửi",
            err: "❌ Lỗi",
            sys: "⚙️ System",
            dbg: "🔍 Debug",
          }).map(([k, v]) => (
            <span key={k} style={{ color: logColor[k] }}>
              {v}
            </span>
          ))}
        </div>

        <div
          ref={logRef}
          style={{
            background: "#0a0a1a",
            borderRadius: 4,
            padding: 12,
            height: 350,
            overflowY: "auto",
            fontFamily: "monospace",
            fontSize: 11,
            lineHeight: 1.6,
          }}
        >
          {logs.length === 0 && (
            <span style={{ color: "#444" }}>
              Bắt đầu bằng cách: Login → Test CORS → Test Negotiate → Kết nối
            </span>
          )}
          {logs.map((l, i) => (
            <div key={i} style={{ color: logColor[l.type] }}>
              <span style={{ color: "#444" }}>[{l.time}]</span> {l.msg}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ── Sub components ───────────────────────────────────
const inputStyle = {
  width: "100%",
  padding: "8px",
  marginBottom: 8,
  background: "#0f3460",
  border: "1px solid #333",
  borderRadius: 4,
  color: "#eee",
  fontSize: 13,
  outline: "none",
  resize: "vertical",
};

const btnStyle = {
  padding: "8px 14px",
  background: "#e94560",
  border: "none",
  borderRadius: 4,
  color: "white",
  cursor: "pointer",
  fontSize: 13,
  marginRight: 8,
  marginBottom: 4,
};

const Input = ({ placeholder, value, onChange, type = "text" }) => (
  <input
    type={type}
    placeholder={placeholder}
    value={value}
    onChange={(e) => onChange(e.target.value)}
    style={inputStyle}
  />
);

const Btn = ({ onClick, children, disabled, style }) => (
  <button
    onClick={onClick}
    disabled={disabled}
    style={{
      ...btnStyle,
      ...(disabled
        ? { background: "#333", cursor: "not-allowed", color: "#666" }
        : {}),
      ...style,
    }}
  >
    {children}
  </button>
);

const Card = ({ title, children }) => (
  <div style={{ background: "#16213e", borderRadius: 8, padding: 16 }}>
    <div
      style={{
        background: "#e94560",
        color: "white",
        padding: "4px 10px",
        borderRadius: 4,
        display: "inline-block",
        fontSize: 11,
        fontWeight: "bold",
        marginBottom: 12,
      }}
    >
      {title}
    </div>
    {children}
  </div>
);
