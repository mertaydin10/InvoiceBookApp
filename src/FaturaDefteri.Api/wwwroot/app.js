const money = (n, c = "TRY") =>
  new Intl.NumberFormat("tr-TR", { style: "currency", currency: c || "TRY" }).format(Number(n || 0));

const statusTr = (s, overdue) => {
  if (overdue) return "Gecikmiş";
  return { Draft: "Taslak", Sent: "Gönderildi", Paid: "Ödendi", Cancelled: "İptal" }[s] || s;
};

const badgeClass = (s, overdue) => {
  if (overdue) return "overdue";
  return { Draft: "draft", Sent: "sent", Paid: "paid", Cancelled: "draft" }[s] || "";
};

const tokenKey = "faturaDefteriToken";
let currency = "TRY";
let editingId = null;

function token() {
  return localStorage.getItem(tokenKey);
}

async function api(path, opts = {}) {
  const headers = { "Content-Type": "application/json", ...(opts.headers || {}) };
  const t = token();
  if (t) headers.Authorization = `Bearer ${t}`;
  const res = await fetch(path, { ...opts, headers });
  if (res.status === 204) return null;
  const body = await res.json().catch(() => ({}));
  if (!res.ok) {
    const err = new Error(body.error || res.statusText);
    err.status = res.status;
    throw err;
  }
  return body;
}

function showApp(loggedIn) {
  document.getElementById("auth-screen").classList.toggle("hidden", loggedIn);
  document.getElementById("app").classList.toggle("hidden", !loggedIn);
}

function logout() {
  localStorage.removeItem(tokenKey);
  showApp(false);
}

async function fillCurrencySelect(selected = "TRY") {
  const sel = document.getElementById("issuer-currency");
  const list = await api("/api/currencies");
  sel.innerHTML = list
    .map((c) => `<option value="${c.code}">${c.name} (${c.symbol})</option>`)
    .join("");
  sel.value = selected;
  if (!sel.value) sel.value = "TRY";
}

function toast(msg) {
  const el = document.getElementById("toast");
  el.textContent = msg;
  el.classList.remove("hidden");
  setTimeout(() => el.classList.add("hidden"), 2800);
}

function show(view) {
  document.querySelectorAll(".view").forEach((v) => v.classList.add("hidden"));
  document.getElementById(`view-${view}`).classList.remove("hidden");
  document.querySelectorAll("nav button").forEach((b) => b.classList.toggle("on", b.dataset.view === view));
}

document.querySelector("nav").addEventListener("click", (e) => {
  const btn = e.target.closest("button[data-view]");
  if (!btn) return;
  show(btn.dataset.view);
  if (btn.dataset.view === "dashboard") loadDashboard();
  if (btn.dataset.view === "invoices") loadInvoices();
  if (btn.dataset.view === "clients") loadClients();
  if (btn.dataset.view === "issuer") loadIssuer();
  if (btn.dataset.view === "profile") loadProfile();
});

function table(headers, rowsHtml) {
  return `<table><thead><tr>${headers.map((h) => `<th>${h}</th>`).join("")}</tr></thead><tbody>${rowsHtml}</tbody></table>`;
}

async function loadDashboard() {
  const s = await api("/api/stats/summary");
  currency = s.currency || "TRY";
  document.getElementById("summary-cards").innerHTML = [
    ["Müşteri", s.clientCount],
    ["Açık fatura", s.openInvoiceCount],
    ["Gecikmiş", s.overdueCount],
    ["Açık tutar", money(s.openGross, currency)],
    ["Gecikmiş tutar", money(s.overdueGross, currency)],
    ["Bu ay tahsilat", money(s.paidThisMonthGross, currency)],
  ]
    .map(([k, v]) => `<article class="card"><span class="muted">${k}</span><strong>${v}</strong></article>`)
    .join("");
  
  const monthlyData = await api("/api/stats/monthly-revenue");
  const maxRevenue = Math.max(...monthlyData.map(m => m.revenue), 1);
  document.getElementById("monthly-chart").innerHTML = monthlyData
    .map(m => {
      const height = (m.revenue / maxRevenue) * 100;
      return `<div class="chart-bar">
        <div class="bar" style="height: ${height}%"></div>
        <div class="bar-label">${m.label}</div>
        <div class="bar-value">${money(m.revenue, currency)}</div>
      </div>`;
    })
    .join("");
  
  const list = await api("/api/invoices");
  renderInvoiceTable("recent-invoices", list.slice(0, 8));
}

function renderInvoiceTable(id, list) {
  const rows = list
    .map(
      (i) => `<tr>
      <td><a href="#" data-open="${i.id}">${i.number}</a></td>
      <td>${i.clientName}</td>
      <td>${i.issueDate}</td>
      <td>${i.dueDate}</td>
      <td><span class="badge ${badgeClass(i.status, i.overdue)}">${statusTr(i.status, i.overdue)}</span></td>
      <td>${money(i.gross, currency)}</td>
    </tr>`
    )
    .join("");
  document.getElementById(id).innerHTML = list.length
    ? table(["No", "Müşteri", "Tarih", "Vade", "Durum", "Tutar"], rows)
    : "<p class='muted'>Kayıt yok.</p>";
}

async function loadInvoices() {
  const clients = await api("/api/clients");
  const sel = document.getElementById("filter-client");
  const current = sel.value;
  sel.innerHTML = `<option value="">Tüm müşteriler</option>` + clients.map((c) => `<option value="${c.id}">${c.name}</option>`).join("");
  sel.value = current;
  const status = document.getElementById("filter-status").value;
  const clientId = sel.value;
  const fromDate = document.getElementById("filter-from-date").value;
  const toDate = document.getElementById("filter-to-date").value;
  const q = new URLSearchParams();
  if (status) q.set("status", status);
  if (clientId) q.set("clientId", clientId);
  if (fromDate) q.set("fromDate", fromDate);
  if (toDate) q.set("toDate", toDate);
  const list = await api(`/api/invoices?${q}`);
  renderInvoiceTable("invoice-table", list);
}

document.getElementById("filter-status").addEventListener("change", loadInvoices);
document.getElementById("filter-client").addEventListener("change", loadInvoices);
document.getElementById("filter-from-date").addEventListener("change", loadInvoices);
document.getElementById("filter-to-date").addEventListener("change", loadInvoices);
document.getElementById("btn-clear-dates").addEventListener("click", () => {
  document.getElementById("filter-from-date").value = "";
  document.getElementById("filter-to-date").value = "";
  loadInvoices();
});

document.getElementById("btn-export-csv").addEventListener("click", async () => {
  const status = document.getElementById("filter-status").value;
  const clientId = document.getElementById("filter-client").value;
  const fromDate = document.getElementById("filter-from-date").value;
  const toDate = document.getElementById("filter-to-date").value;
  const q = new URLSearchParams();
  if (status) q.set("status", status);
  if (clientId) q.set("clientId", clientId);
  if (fromDate) q.set("fromDate", fromDate);
  if (toDate) q.set("toDate", toDate);
  const list = await api(`/api/invoices?${q}`);
  
  const csv = [
    ["Fatura No", "Müşteri", "Tarih", "Vade", "Durum", "Tutar"].join(","),
    ...list.map(i => [
      i.number,
      `"${i.clientName}"`,
      i.issueDate,
      i.dueDate,
      statusTr(i.status, i.overdue),
      i.gross
    ].join(","))
  ].join("\n");
  
  const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `faturalar-${new Date().toISOString().split("T")[0]}.csv`;
  link.click();
  toast("CSV dosyası indirildi");
});

document.body.addEventListener("click", (e) => {
  const a = e.target.closest("[data-open]");
  if (!a) return;
  e.preventDefault();
  openInvoice(Number(a.dataset.open));
});

async function loadClients() {
  const list = await api("/api/clients");
  const rows = list
    .map(
      (c) => `<tr>
      <td>${c.name}</td>
      <td>${c.taxNumber || "—"}</td>
      <td>${c.email || "—"}</td>
      <td>${c.phone || "—"}</td>
      <td>
        <button type="button" data-edit-client='${JSON.stringify(c)}'>Düzenle</button>
        <button type="button" class="danger" data-del-client="${c.id}">Sil</button>
      </td>
    </tr>`
    )
    .join("");
  document.getElementById("client-table").innerHTML = list.length
    ? table(["Ad", "Vergi no", "E-posta", "Telefon", ""], rows)
    : "<p class='muted'>Müşteri yok.</p>";
}

document.getElementById("client-table").addEventListener("click", async (e) => {
  const edit = e.target.closest("[data-edit-client]");
  const del = e.target.closest("[data-del-client]");
  if (edit) openClientDialog(JSON.parse(edit.dataset.editClient));
  if (del) {
    if (!confirm("Müşteri silinsin mi?")) return;
    try {
      await api(`/api/clients/${del.dataset.delClient}`, { method: "DELETE" });
      toast("Silindi");
      loadClients();
    } catch (err) {
      toast(err.message);
    }
  }
});

function openClientDialog(c = {}) {
  const form = document.getElementById("client-form");
  form.id.value = c.id || "";
  form.name.value = c.name || "";
  form.taxNumber.value = c.taxNumber || "";
  form.email.value = c.email || "";
  form.phone.value = c.phone || "";
  form.address.value = c.address || "";
  document.getElementById("client-dialog-title").textContent = c.id ? "Müşteri düzenle" : "Yeni müşteri";
  document.getElementById("client-dialog").showModal();
}

document.getElementById("btn-new-client").addEventListener("click", () => openClientDialog());
document.getElementById("btn-client-close").addEventListener("click", () => document.getElementById("client-dialog").close());

document.getElementById("client-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = e.target;
  const payload = {
    name: f.name.value,
    taxNumber: f.taxNumber.value,
    email: f.email.value,
    phone: f.phone.value,
    address: f.address.value,
  };
  try {
    if (f.id.value) await api(`/api/clients/${f.id.value}`, { method: "PUT", body: JSON.stringify(payload) });
    else await api("/api/clients", { method: "POST", body: JSON.stringify(payload) });
    document.getElementById("client-dialog").close();
    toast("Kaydedildi");
    loadClients();
  } catch (err) {
    toast(err.message);
  }
});

async function loadIssuer() {
  const i = await api("/api/issuer");
  const f = document.getElementById("issuer-form");
  await fillCurrencySelect(i.currency || "TRY");
  f.tradeName.value = i.tradeName || "";
  f.taxOffice.value = i.taxOffice || "";
  f.taxNumber.value = i.taxNumber || "";
  f.email.value = i.email || "";
  f.phone.value = i.phone || "";
  f.iban.value = i.iban || "";
  f.currency.value = i.currency || "TRY";
  f.address.value = i.address || "";
}

document.getElementById("issuer-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = e.target;
  try {
    const saved = await api("/api/issuer", {
      method: "PUT",
      body: JSON.stringify({
        tradeName: f.tradeName.value,
        taxOffice: f.taxOffice.value,
        taxNumber: f.taxNumber.value,
        email: f.email.value,
        phone: f.phone.value,
        iban: f.iban.value,
        currency: f.currency.value,
        address: f.address.value,
      }),
    });
    currency = saved.currency;
    toast("Firma kaydedildi");
  } catch (err) {
    toast(err.message);
  }
});

function lineRow(line = {}) {
  const wrap = document.createElement("div");
  wrap.className = "line-row";
  wrap.innerHTML = `
    <input placeholder="Açıklama" value="${line.description || ""}" data-k="description" />
    <input type="number" min="0.01" step="0.01" placeholder="Miktar" value="${line.quantity ?? 1}" data-k="quantity" />
    <input type="number" min="0" step="0.01" placeholder="Birim fiyat" value="${line.unitPrice ?? 0}" data-k="unitPrice" />
    <input type="number" min="0" max="100" step="0.01" placeholder="KDV %" value="${line.vatRate ?? 20}" data-k="vatRate" />
    <button type="button" class="danger" data-remove-line>Sil</button>`;
  wrap.querySelectorAll("input").forEach((i) => i.addEventListener("input", updateTotals));
  wrap.querySelector("[data-remove-line]").addEventListener("click", () => {
    wrap.remove();
    updateTotals();
  });
  return wrap;
}

function collectLines() {
  return [...document.querySelectorAll("#lines .line-row")].map((row) => {
    const get = (k) => row.querySelector(`[data-k="${k}"]`).value;
    return {
      description: get("description"),
      quantity: Number(get("quantity")),
      unitPrice: Number(get("unitPrice")),
      vatRate: Number(get("vatRate")),
    };
  });
}

function updateTotals() {
  let net = 0,
    vat = 0;
  for (const l of collectLines()) {
    const n = Math.round(l.quantity * l.unitPrice * 100) / 100;
    const v = Math.round(n * (l.vatRate / 100) * 100) / 100;
    net += n;
    vat += v;
  }
  document.getElementById("editor-totals").textContent = `Ara toplam ${money(net, currency)} · KDV ${money(vat, currency)} · Genel ${money(net + vat, currency)}`;
}

async function fillClientSelect(selected) {
  const clients = await api("/api/clients");
  const sel = document.getElementById("editor-client");
  sel.innerHTML = clients.map((c) => `<option value="${c.id}">${c.name}</option>`).join("");
  if (selected) sel.value = selected;
}

function todayStr() {
  return new Date().toISOString().slice(0, 10);
}

async function newInvoice() {
  editingId = null;
  document.getElementById("editor-title").textContent = "Yeni fatura";
  const f = document.getElementById("invoice-form");
  f.reset();
  f.issueDate.value = todayStr();
  f.dueDate.value = todayStr();
  document.getElementById("lines").innerHTML = "";
  document.getElementById("lines").append(lineRow());
  f.querySelectorAll("input, select, textarea, #btn-add-line").forEach((el) => {
    el.disabled = false;
  });
  await fillClientSelect();
  setActions({ send: false, pay: false, print: false, cancel: false, del: false });
  updateTotals();
  show("editor");
}

function setActions(flags) {
  document.getElementById("btn-send").classList.toggle("hidden", !flags.send);
  document.getElementById("btn-pay").classList.toggle("hidden", !flags.pay);
  document.getElementById("btn-print").classList.toggle("hidden", !flags.print);
  document.getElementById("btn-cancel-inv").classList.toggle("hidden", !flags.cancel);
  document.getElementById("btn-delete-inv").classList.toggle("hidden", !flags.del);
}

async function openInvoice(id) {
  const inv = await api(`/api/invoices/${id}`);
  editingId = inv.id;
  document.getElementById("editor-title").textContent = inv.number;
  const f = document.getElementById("invoice-form");
  await fillClientSelect(inv.clientId);
  f.issueDate.value = inv.issueDate;
  f.dueDate.value = inv.dueDate;
  f.notes.value = inv.notes || "";
  const box = document.getElementById("lines");
  box.innerHTML = "";
  inv.lines.forEach((l) => box.append(lineRow(l)));
  setActions({
    send: inv.status === "Draft",
    pay: inv.status === "Draft" || inv.status === "Sent",
    print: true,
    cancel: inv.status !== "Paid" && inv.status !== "Cancelled",
    del: inv.status === "Draft",
  });
  const locked = inv.status === "Paid" || inv.status === "Cancelled";
  f.querySelectorAll("input, select, textarea, #btn-add-line").forEach((el) => {
    if (el.id === "btn-add-line") el.disabled = locked;
    else el.disabled = locked;
  });
  updateTotals();
  show("editor");
}

document.getElementById("btn-new-invoice").addEventListener("click", newInvoice);
document.getElementById("btn-add-line").addEventListener("click", () => {
  document.getElementById("lines").append(lineRow());
  updateTotals();
});
document.getElementById("btn-back-invoices").addEventListener("click", () => {
  show("invoices");
  loadInvoices();
});

document.getElementById("invoice-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = e.target;
  const payload = {
    clientId: Number(f.clientId.value),
    issueDate: f.issueDate.value,
    dueDate: f.dueDate.value,
    notes: f.notes.value,
    lines: collectLines(),
  };
  try {
    const saved = editingId
      ? await api(`/api/invoices/${editingId}`, { method: "PUT", body: JSON.stringify(payload) })
      : await api("/api/invoices", { method: "POST", body: JSON.stringify(payload) });
    toast("Kaydedildi");
    await openInvoice(saved.id);
  } catch (err) {
    toast(err.message);
  }
});

async function act(path) {
  try {
    const saved = await api(path, { method: "POST" });
    toast("Güncellendi");
    await openInvoice(saved.id);
  } catch (err) {
    toast(err.message);
  }
}

document.getElementById("btn-send").addEventListener("click", () => act(`/api/invoices/${editingId}/send`));
document.getElementById("btn-pay").addEventListener("click", () => act(`/api/invoices/${editingId}/pay`));
document.getElementById("btn-cancel-inv").addEventListener("click", () => act(`/api/invoices/${editingId}/cancel`));
document.getElementById("btn-delete-inv").addEventListener("click", async () => {
  if (!confirm("Taslak silinsin mi?")) return;
  try {
    await api(`/api/invoices/${editingId}`, { method: "DELETE" });
    toast("Silindi");
    show("invoices");
    loadInvoices();
  } catch (err) {
    toast(err.message);
  }
});

document.getElementById("btn-print").addEventListener("click", async () => {
  const inv = await api(`/api/invoices/${editingId}`);
  const issuer = await api("/api/issuer");
  const w = window.open("", "_blank");
  w.document.write(`<!DOCTYPE html><html lang="tr"><head><meta charset="utf-8"><title>${inv.number}</title>
  <style>
    body{font-family:Georgia,serif;margin:32px;color:#111}
    h1{margin:0} table{width:100%;border-collapse:collapse;margin-top:16px}
    th,td{border-bottom:1px solid #ccc;padding:8px;text-align:left}
    .muted{color:#555} .right{text-align:right} header{display:flex;justify-content:space-between}
  </style></head><body>
  <header>
    <div>
      <h1>${issuer.tradeName || "Fatura"}</h1>
      <p class="muted">${issuer.address || ""}<br>${issuer.taxOffice || ""} ${issuer.taxNumber || ""}<br>${issuer.email || ""} ${issuer.phone || ""}</p>
    </div>
    <div class="right">
      <strong>${inv.number}</strong><br>
      Tarih: ${inv.issueDate}<br>
      Vade: ${inv.dueDate}<br>
      Durum: ${statusTr(inv.status, inv.overdue)}
    </div>
  </header>
  <p><strong>Müşteri:</strong> ${inv.clientName}</p>
  <table><thead><tr><th>Açıklama</th><th>Miktar</th><th>Birim</th><th>KDV</th><th>Tutar</th></tr></thead><tbody>
  ${inv.lines
    .map(
      (l) =>
        `<tr><td>${l.description}</td><td>${l.quantity}</td><td>${money(l.unitPrice, currency)}</td><td>%${l.vatRate}</td><td>${money(l.gross, currency)}</td></tr>`
    )
    .join("")}
  </tbody></table>
  <p class="right">Ara toplam ${money(inv.net, currency)}<br>KDV ${money(inv.vat, currency)}<br><strong>Genel toplam ${money(inv.gross, currency)}</strong></p>
  ${issuer.iban ? `<p>IBAN: ${issuer.iban}</p>` : ""}
  ${inv.notes ? `<p>${inv.notes}</p>` : ""}
  </body></html>`);
  w.document.close();
  w.focus();
  w.print();
});

async function loadProfile() {
  try {
    const user = await api("/api/auth/me");
    const f = document.getElementById("profile-form");
    f.email.value = user.email;
    f.displayName.value = user.displayName;
  } catch (err) {
    toast(err.message);
  }
}

document.getElementById("profile-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = e.target;
  try {
    await api("/api/auth/me", {
      method: "PUT",
      body: JSON.stringify({ displayName: f.displayName.value }),
    });
    toast("Profil güncellendi");
  } catch (err) {
    toast(err.message);
  }
});

document.getElementById("password-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const f = e.target;
  if (f.newPassword.value !== f.confirmPassword.value) {
    toast("Yeni şifreler eşleşmiyor");
    return;
  }
  try {
    await api("/api/auth/change-password", {
      method: "POST",
      body: JSON.stringify({
        currentPassword: f.currentPassword.value,
        newPassword: f.newPassword.value,
      }),
    });
    toast("Şifre değiştirildi");
    f.reset();
  } catch (err) {
    toast(err.message);
  }
});

async function enterApp() {
  showApp(true);
  await loadDashboard();
}

document.getElementById("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errEl = document.getElementById("auth-error");
  errEl.textContent = "";
  const f = e.target;
  try {
    const data = await api("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: f.email.value, password: f.password.value }),
    });
    localStorage.setItem(tokenKey, data.token);
    await enterApp();
  } catch (err) {
    errEl.textContent = err.message;
  }
});

document.getElementById("register-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const errEl = document.getElementById("auth-error");
  errEl.textContent = "";
  const f = e.target;
  try {
    const data = await api("/api/auth/register", {
      method: "POST",
      body: JSON.stringify({
        email: f.email.value,
        password: f.password.value,
        displayName: f.displayName.value || null,
      }),
    });
    localStorage.setItem(tokenKey, data.token);
    await enterApp();
  } catch (err) {
    errEl.textContent = err.message;
  }
});

document.getElementById("btn-logout").addEventListener("click", logout);

if (token()) enterApp().catch((err) => {
  toast(err.message);
  if (err.status === 401) logout();
});
else showApp(false);
