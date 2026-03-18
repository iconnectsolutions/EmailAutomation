const API = '/api';

async function checkAuth() {
  const statusEl = document.getElementById('authStatus');
  const connectBtn = document.getElementById('connectBtn');
  const connectedLabel = document.getElementById('connectedLabel');

  try {
    const res = await fetch(`${API}/auth/status`);
    const data = await res.json();
    if (data.authenticated) {
      statusEl.className = 'auth-status connected';
      statusEl.querySelector('.status-text').textContent = 'Connected';
      connectBtn.style.display = 'none';
      document.getElementById('reconnectBtn').style.display = 'inline-block';
      connectedLabel.style.display = 'inline';
    } else {
      statusEl.className = 'auth-status disconnected';
      statusEl.querySelector('.status-text').textContent = 'Not connected';
      connectBtn.style.display = 'inline-block';
      document.getElementById('reconnectBtn').style.display = 'none';
      connectedLabel.style.display = 'none';
    }
  } catch (e) {
    statusEl.className = 'auth-status disconnected';
    statusEl.querySelector('.status-text').textContent = 'Error';
    connectBtn.style.display = 'inline-block';
    document.getElementById('reconnectBtn').style.display = 'none';
  }
}

async function reconnect() {
  try {
    await fetch(`${API}/auth/disconnect`, { method: 'POST' });
    window.location.href = (await fetch(`${API}/auth/login-url`).then(r => r.json())).url;
  } catch (e) {
    alert('Reconnect failed: ' + e.message);
  }
}

async function getLoginUrl() {
  const res = await fetch(`${API}/auth/login-url`);
  const data = await res.json();
  window.location.href = data.url;
}

async function loadTemplates() {
  const listEl = document.getElementById('templatesList');
  const selectEl = document.getElementById('templateSelect');
  try {
    const res = await fetch(`${API}/templates`);
    const templates = await res.json() ?? [];
    const arr = Array.isArray(templates) ? templates : [];

    listEl.innerHTML = arr.length === 0
      ? '<p class="muted">No templates yet. Add one above.</p>'
      : arr.map(t => `
          <div class="template-item">
            <strong>${escapeHtml(t.name)}</strong>
            <span class="muted">— ${escapeHtml(t.subject)}</span>
            <button class="btn btn-small btn-secondary" data-id="${t.id}" data-action="edit">Edit</button>
            <button class="btn btn-small btn-danger" data-id="${t.id}" data-action="delete">Delete</button>
          </div>
        `).join('');

    selectEl.innerHTML = '<option value="">-- Select a template --</option>' +
      arr.map(t => `<option value="${t.id}">${escapeHtml(t.name)}</option>`).join('');

    selectEl.dispatchEvent(new Event('change'));
  } catch (e) {
    listEl.innerHTML = `<p class="muted">Error loading templates</p>`;
  }
}

async function loadBatchesDropdown() {
  const select = document.getElementById('batchSelect');
  const hint = document.getElementById('batchHint');
  try {
    const res = await fetch(`${API}/batches`);
    const batches = await res.json() ?? [];
    select.innerHTML = '<option value="">-- Select a batch --</option>' +
      (Array.isArray(batches) ? batches : []).map(b => `<option value="${b.id}">${escapeHtml(b.name || '')} (${b.contactCount ?? 0} contacts) - ${b.createdAt ? new Date(b.createdAt).toLocaleString() : '-'}</option>`).join('');
    if (hint) {
      hint.textContent = batches.length === 0 ? 'No batches yet. Create one from Contacts.' : '';
      hint.className = batches.length === 0 ? 'muted' : '';
    }
  } catch (e) {
    select.innerHTML = '<option value="">Error loading batches</option>';
    if (hint) hint.textContent = 'Could not load batches. Check console for errors.';
  }
}

async function loadBatchList() {
  const container = document.getElementById('batchesContainer');
  if (!container) {
    // No summary list on the page anymore; nothing to render.
    return;
  }
  try {
    const res = await fetch(`${API}/batches`);
    const batches = await res.json() ?? [];
    const arr = Array.isArray(batches) ? batches : [];
    if (arr.length === 0) {
      container.innerHTML = '<p class="muted">No batches yet. Create one from the Contacts page.</p>';
      return;
    }
    container.innerHTML = arr.map(b => `
      <div class="job-item">
        <strong>${escapeHtml(b.name)}</strong> - ${b.contactCount ?? 0} contacts
        ${b.createdAt ? ` - ${formatDate(b.createdAt)}` : ''}
      </div>
    `).join('');
  } catch (e) {
    container.innerHTML = '<p class="muted">Error loading batches</p>';
  }
}

async function loadBatchContacts(batchId) {
  const container = document.getElementById('recipientsContainer');
  if (!batchId) {
    container.innerHTML = '<p class="muted">Select a batch to view its contacts.</p>';
    return;
  }
  try {
    const res = await fetch(`${API}/batches/${batchId}/contacts`);
    const contacts = await res.json();
    if (contacts.length === 0) {
      container.innerHTML = '<p class="muted">No contacts in this batch</p>';
      return;
    }
    const mailHeaders = Array.from({ length: 15 }, (_, i) => `Mail${i + 1}`);
    container.innerHTML = `
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Email</th>
              <th>Name</th>
              ${mailHeaders.map(h => `<th>${h}</th>`).join('')}
              <th>Ignore</th>
            </tr>
          </thead>
          <tbody>
            ${contacts.map(r => `
              <tr>
                <td>${escapeHtml(r.email)}</td>
                <td>${escapeHtml(r.name)}</td>
                ${mailHeaders.map((_, idx) => {
                  const key = `mail${idx + 1}Date`;
                  return `<td>${r[key] ? formatDate(r[key]) : '-'}</td>`;
                }).join('')}
                <td>${r.ignore ? 'Yes' : 'No'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  } catch (e) {
    container.innerHTML = `<p class="muted">Error: ${escapeHtml(e.message)}</p>`;
  }
}

async function loadJobs() {
  const container = document.getElementById('jobsContainer');
  try {
    const res = await fetch(`${API}/send/jobs`);
    const jobs = await res.json();
    if (jobs.length === 0) {
      container.innerHTML = '<p class="muted">No jobs yet</p>';
      return;
    }
    container.innerHTML = jobs.map(j => `
      <div class="job-item">
        <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:0.75rem;">
          <div>
            <strong>Job ${j.id}</strong> — <span class="muted">Batch ${j.batchId}</span> — ${escapeHtml(j.templateSubject)}<br>
            <span class="job-status ${j.status.toLowerCase()}">${j.status}</span>
            ${j.sentCount !== undefined ? ` - ${j.sentCount} sent` : ''}
            ${j.completedAt ? ` - ${formatDate(j.completedAt)}` : ''}
            ${j.retryOfJobId ? ` - <span class="muted">retry of job ${j.retryOfJobId}</span>` : ''}
            ${j.errorMessage ? `<br><small style="color:#991b1b">${escapeHtml(j.errorMessage)}</small>` : ''}
          </div>
          <div style="display:flex;gap:0.4rem;flex-wrap:wrap;justify-content:flex-end;">
            <button class="btn btn-small btn-secondary" data-action="job-details" data-job-id="${j.id}">View details</button>
            <button class="btn btn-small btn-primary" data-action="job-retry-failed" data-job-id="${j.id}">Retry failed</button>
          </div>
        </div>
      </div>
    `).join('');
  } catch (e) {
    container.innerHTML = `<p class="muted">Error loading jobs</p>`;
  }
}

let currentJobDetails = null; // { id, batchId, templateSubject, status, completedAt, retryOfJobId, templateId }

function openJobDetailsModal() {
  const modal = document.getElementById('jobDetailsModal');
  modal.style.display = 'flex';
  modal.setAttribute('aria-hidden', 'false');
}

function closeJobDetailsModal() {
  const modal = document.getElementById('jobDetailsModal');
  modal.style.display = 'none';
  modal.setAttribute('aria-hidden', 'true');
  currentJobDetails = null;
}

function renderJobDetailsMeta(job) {
  const meta = document.getElementById('jobDetailsMeta');
  const parts = [
    `Job ${job.id}`,
    `Batch ${job.batchId}`,
    job.templateSubject ? `Template: ${job.templateSubject}` : null,
    job.status ? `Status: ${job.status}` : null,
    job.completedAt ? `Completed: ${formatDate(job.completedAt)}` : null,
    job.retryOfJobId ? `Retry of job ${job.retryOfJobId}` : null
  ].filter(Boolean);
  meta.textContent = parts.join(' • ');
}

function renderJobStatusSummary(rows) {
  const el = document.getElementById('jobStatusSummary');
  const counts = { Sent: 0, Failed: 0, Ignored: 0 };
  rows.forEach(r => {
    if (r.status === 'Sent') counts.Sent++;
    else if (r.status === 'Failed') counts.Failed++;
    else if (r.status === 'Ignored') counts.Ignored++;
  });
  const total = rows.length;
  el.innerHTML = `
    <span class="pill sent"><span class="dot"></span>Sent <strong>${counts.Sent}</strong></span>
    <span class="pill failed"><span class="dot"></span>Failed <strong>${counts.Failed}</strong></span>
    <span class="pill ignored"><span class="dot"></span>Ignored <strong>${counts.Ignored}</strong></span>
    <span class="pill"><span class="dot"></span>Total <strong>${total}</strong></span>
  `;

  const retryBtn = document.getElementById('jobRetryFailedBtn');
  const retryHint = document.getElementById('jobRetryHint');
  retryBtn.disabled = counts.Failed === 0;
  retryHint.textContent = counts.Failed === 0 ? 'No failed recipients to retry.' : `${counts.Failed} failed recipients can be retried.`;
}

function renderRecipientsTable(rows) {
  const container = document.getElementById('jobRecipientsContainer');
  if (!rows.length) {
    container.innerHTML = '<p class="muted">No recipients found for this filter.</p>';
    return;
  }
  container.innerHTML = `
    <table>
      <thead>
        <tr>
          <th>Email</th>
          <th>Name</th>
          <th>Status</th>
          <th>Reason</th>
          <th>Attempts</th>
          <th>Last Attempt</th>
        </tr>
      </thead>
      <tbody>
        ${rows.map(r => `
          <tr>
            <td>${escapeHtml(r.email)}</td>
            <td>${escapeHtml(r.name)}</td>
            <td><span class="job-status ${escapeHtml((r.status || '').toLowerCase())}">${escapeHtml(r.status || '')}</span></td>
            <td>
              ${r.reasonCode ? `<strong>${escapeHtml(r.reasonCode)}</strong>` : '-'}
              ${r.reasonMessage ? `<br><small class="muted">${escapeHtml(r.reasonMessage)}</small>` : ''}
            </td>
            <td>${r.attemptCount ?? 0}</td>
            <td>${r.lastAttemptAt ? formatDate(r.lastAttemptAt) : '-'}</td>
          </tr>
        `).join('')}
      </tbody>
    </table>
  `;
}

async function fetchJobRecipients(jobId, status = '') {
  const qs = status ? `?status=${encodeURIComponent(status)}` : '';
  const res = await fetch(`${API}/send/jobs/${jobId}/recipients${qs}`);
  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || 'Failed to load job recipients');
  }
  const rows = await res.json();
  return Array.isArray(rows) ? rows : [];
}

async function showJobDetails(jobId) {
  openJobDetailsModal();
  const container = document.getElementById('jobRecipientsContainer');
  container.innerHTML = '<p class="muted">Loading...</p>';

  // Load job info from list endpoint (simple + consistent with current UI).
  const jobsRes = await fetch(`${API}/send/jobs`);
  const jobs = await jobsRes.json().catch(() => []);
  const job = (Array.isArray(jobs) ? jobs : []).find(j => j.id === jobId);
  currentJobDetails = job || { id: jobId };
  renderJobDetailsMeta(currentJobDetails);

  const allRows = await fetchJobRecipients(jobId, '');
  renderJobStatusSummary(allRows);
  renderRecipientsTable(allRows);

  // Reset tabs to All.
  document.querySelectorAll('#jobRecipientTabs .tab').forEach(t => t.classList.toggle('active', t.dataset.status === ''));
}

async function retryFailed(jobId) {
  const btn = document.getElementById('jobRetryFailedBtn');
  const hint = document.getElementById('jobRetryHint');
  if (btn) btn.disabled = true;
  if (hint) hint.textContent = 'Retrying failed recipients...';

  const res = await fetch(`${API}/send/jobs/${jobId}/retry-failed`, { method: 'POST' });
  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    if (hint) hint.textContent = data.error || 'Retry failed.';
    if (btn) btn.disabled = false;
    return;
  }

  if (hint) hint.textContent = `Retry job created: ${data.id}. Refreshing...`;
  await loadJobs();
  // Show details of the newly created retry job.
  if (data?.id) {
    await showJobDetails(data.id);
  }
}

function formatDate(s) {
  try {
    return new Date(s).toLocaleDateString();
  } catch {
    return s;
  }
}

function escapeHtml(s) {
  if (!s) return '';
  const div = document.createElement('div');
  div.textContent = s;
  return div.innerHTML;
}

function showTemplateForm(editId = null) {
  const form = document.getElementById('templateForm');
  form.style.display = 'block';
  document.getElementById('templateName').value = '';
  document.getElementById('templateSubject').value = '';
  document.getElementById('templateBody').value = '';
  form.dataset.editId = editId || '';
}

function hideTemplateForm() {
  document.getElementById('templateForm').style.display = 'none';
}

async function saveTemplate() {
  const name = document.getElementById('templateName').value.trim();
  const subject = document.getElementById('templateSubject').value.trim();
  const body = document.getElementById('templateBody').value;
  const editId = document.getElementById('templateForm').dataset.editId;

  if (!name || !subject) {
    alert('Name and Subject are required');
    return;
  }

  try {
    if (editId) {
      await fetch(`${API}/templates/${editId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, subject, body })
      });
    } else {
      await fetch(`${API}/templates`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, subject, body })
      });
    }
    hideTemplateForm();
    await loadTemplates();
  } catch (e) {
    alert('Failed to save template: ' + e.message);
  }
}

async function loadContacts(page = 1) {
  const container = document.getElementById('contactsContainer');
  const searchInput = document.getElementById('contactsSearch');
  const createBtn = document.getElementById('createBatchFromContactsBtn');
  const search = searchInput?.value?.trim() || '';
  try {
    const res = await fetch(`${API}/contacts?search=${encodeURIComponent(search)}&page=${page}&pageSize=50`);
    const data = await res.json();
    const items = data.items || [];
    if (!items.length) {
      container.innerHTML = '<p class="muted">No contacts found.</p>';
      createBtn.disabled = true;
      return;
    }
    const mailHeaders = Array.from({ length: 15 }, (_, i) => `Mail${i + 1}`);
    createBtn.disabled = true;
    container.innerHTML = `
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th><input type="checkbox" id="contactsSelectAll" /></th>
              <th>Email</th>
              <th>Name</th>
              ${mailHeaders.map(h => `<th>${h}</th>`).join('')}
              <th>Ignore</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            ${items.map(c => `
              <tr>
                <td><input type="checkbox" class="contact-select" data-id="${c.id}" /></td>
                <td>${escapeHtml(c.email)}</td>
                <td>${escapeHtml(c.name)}</td>
                ${mailHeaders.map((_, idx) => {
                  const key = `mail${idx + 1}Date`;
                  return `<td>${c[key] ? formatDate(c[key]) : '-'}</td>`;
                }).join('')}
                <td>${c.ignore ? 'Yes' : 'No'}</td>
                <td><button class="btn btn-small btn-danger contact-delete" data-id="${c.id}">Delete</button></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;

    const selectAll = document.getElementById('contactsSelectAll');
    selectAll?.addEventListener('change', e => {
      const checked = e.target.checked;
      document.querySelectorAll('.contact-select').forEach(cb => {
        cb.checked = checked;
      });
      updateCreateBatchButtonState();
    });

    container.querySelectorAll('.contact-select').forEach(cb => {
      cb.addEventListener('change', updateCreateBatchButtonState);
    });

    container.querySelectorAll('.contact-delete').forEach(btn => {
      btn.addEventListener('click', async e => {
        const id = e.target.dataset.id;
        if (!id) return;
        if (!confirm('Delete this contact?')) return;
        try {
          const res = await fetch(`${API}/contacts/${id}`, { method: 'DELETE' });
          if (!res.ok && res.status !== 404) {
            const data = await res.json().catch(() => ({}));
            alert(data.error || 'Failed to delete contact');
            return;
          }
          await loadContacts(page);
        } catch (err) {
          alert('Failed to delete contact');
        }
      });
    });
  } catch (e) {
    container.innerHTML = `<p class="muted">Error loading contacts</p>`;
    createBtn.disabled = true;
  }
}

function getSelectedContactIds() {
  const ids = [];
  document.querySelectorAll('.contact-select:checked').forEach(cb => {
    const id = parseInt(cb.dataset.id, 10);
    if (!isNaN(id)) {
      ids.push(id);
    }
  });
  return ids;
}

function updateCreateBatchButtonState() {
  const createBtn = document.getElementById('createBatchFromContactsBtn');
  const selected = getSelectedContactIds();
  createBtn.disabled = selected.length === 0;
}

function openBatchModal() {
  const ids = getSelectedContactIds();
  if (ids.length === 0) return;

  const modal = document.getElementById('batchModal');
  const nameInput = document.getElementById('batchModalName');
  const countEl = document.getElementById('batchModalCount');

  countEl.textContent = ids.length.toString();
  nameInput.value = '';
  modal.style.display = 'flex';
  nameInput.focus();
}

function closeBatchModal() {
  const modal = document.getElementById('batchModal');
  modal.style.display = 'none';
}

async function createBatchFromContacts() {
  const ids = getSelectedContactIds();
  if (ids.length === 0) return;

  const nameInput = document.getElementById('batchModalName');
  const name = nameInput.value?.trim();
  if (!name) {
    nameInput.focus();
    return;
  }

  try {
    const res = await fetch(`${API}/contacts/batches`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name: name.trim(), contactIds: ids })
    });
    const data = await res.json();
    if (!res.ok) {
      alert(data.error || 'Failed to create batch');
      return;
    }
    await loadBatchList();
    await loadBatchesDropdown();
    closeBatchModal();
  } catch (e) {
    alert('Failed to create batch: ' + e.message);
  }
}

async function uploadFile() {
  const fileInput = document.getElementById('importFile');
  const resultEl = document.getElementById('uploadResult');
  if (!fileInput.files?.length) {
    resultEl.className = 'result error';
    resultEl.textContent = 'Please select a file';
    return;
  }

  const file = fileInput.files[0];
  const isCsv = file.name.toLowerCase().endsWith('.csv');
  const isExcel = file.name.toLowerCase().endsWith('.xlsx');
  if (!isCsv && !isExcel) {
    resultEl.className = 'result error';
    resultEl.textContent = 'File must be CSV or Excel (.xlsx)';
    return;
  }

  const formData = new FormData();
  formData.append('file', file);

  try {
    const endpoint = isCsv ? `${API}/import/csv` : `${API}/import/excel`;
    const res = await fetch(endpoint, { method: 'POST', body: formData });
    const data = await res.json();
    if (!res.ok) {
      resultEl.className = 'result error';
      resultEl.textContent = data.error || 'Upload failed';
      return;
    }
    resultEl.className = 'result success';
    let msg = `Imported ${data.rowCount} contacts from ${data.fileName}`;
    if (data.skippedCount > 0) {
      msg += ` (${data.skippedCount} skipped)`;
      if (data.errors?.length) {
        msg += '. First few: ' + data.errors.slice(0, 3).join('; ');
      }
    }
    resultEl.textContent = msg;
    fileInput.value = '';
    await loadContacts();
  } catch (e) {
    resultEl.className = 'result error';
    resultEl.textContent = e.message || 'Upload failed';
  }
}

async function sendEmails() {
  const batchId = document.getElementById('batchSelect').value;
  const templateId = document.getElementById('templateSelect').value;
  const resultEl = document.getElementById('sendResult');
  const sendBtn = document.getElementById('sendBtn');

  if (!batchId || !templateId) {
    resultEl.className = 'result error';
    resultEl.textContent = 'Please select a batch and a template';
    return;
  }

  sendBtn.disabled = true;
  resultEl.className = 'result info';
  resultEl.textContent = 'Sending...';

  try {
    const res = await fetch(`${API}/send`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ batchId: parseInt(batchId), templateId: parseInt(templateId) })
    });
    const data = await res.json();
    if (!res.ok) {
      resultEl.className = 'result error';
      resultEl.textContent = data.error || 'Send failed';
      return;
    }
    resultEl.className = 'result success';
    resultEl.textContent = `Status: ${data.status}. Sent: ${data.sentCount}${data.errorMessage ? '. ' + data.errorMessage : ''}`;
    await loadBatchContacts(batchId);
    await loadJobs();
  } catch (e) {
    resultEl.className = 'result error';
    resultEl.textContent = e.message || 'Send failed';
  } finally {
    sendBtn.disabled = false;
  }
}

function showPage(page) {
  document.querySelectorAll('.page').forEach(p => {
    p.style.display = p.id === `page-${page}` ? 'block' : 'none';
  });
  document.querySelectorAll('.nav-link').forEach(btn => {
    btn.classList.toggle('active', btn.dataset.page === page);
  });
}

function init() {
  const params = new URLSearchParams(window.location.search);
  const err = params.get('error');
  if (err) {
    alert('Auth Error: ' + decodeURIComponent(err));
    window.history.replaceState({}, '', '/');
  }
  if (params.get('connected') === '1') {
    window.history.replaceState({}, '', '/');
  }

  checkAuth();
  loadTemplates();
  loadContacts();
  loadBatchList();
  loadBatchesDropdown();
  loadJobs();

  document.getElementById('connectBtn').addEventListener('click', getLoginUrl);
  document.getElementById('reconnectBtn').addEventListener('click', reconnect);
  document.getElementById('addTemplateBtn').addEventListener('click', () => showTemplateForm());
  document.getElementById('saveTemplateBtn').addEventListener('click', saveTemplate);
  document.getElementById('cancelTemplateBtn').addEventListener('click', hideTemplateForm);
  document.getElementById('uploadBtn').addEventListener('click', uploadFile);
  document.getElementById('sendBtn').addEventListener('click', sendEmails);

  document.querySelectorAll('.nav-link').forEach(btn => {
    btn.addEventListener('click', () => {
      const page = btn.dataset.page;
      showPage(page);
      if (page === 'contacts') {
        loadContacts();
      } else if (page === 'batches') {
        loadBatchList();
        loadBatchesDropdown();
      } else if (page === 'jobs') {
        loadJobs();
      } else if (page === 'templates') {
        loadTemplates();
      }
    });
  });

  document.getElementById('batchSelect').addEventListener('change', e => {
    loadBatchContacts(e.target.value);
    updateSendButtonState();
  });
  document.getElementById('templateSelect').addEventListener('change', updateSendButtonState);

  document.getElementById('importFile').addEventListener('change', e => {
    document.getElementById('uploadBtn').disabled = !e.target.files?.length;
  });

  document.getElementById('contactsSearch').addEventListener('input', () => {
    loadContacts();
  });

  document.getElementById('createBatchFromContactsBtn').addEventListener('click', openBatchModal);
  document.getElementById('batchModalCancel').addEventListener('click', closeBatchModal);
  document.getElementById('batchModalCreate').addEventListener('click', createBatchFromContacts);
  document.getElementById('batchModal').addEventListener('click', e => {
    if (e.target.id === 'batchModal') {
      closeBatchModal();
    }
  });

  document.getElementById('templatesList').addEventListener('click', async e => {
    const btn = e.target.closest('button');
    if (!btn) return;
    const id = btn.dataset.id;
    const action = btn.dataset.action;
    if (action === 'edit' && id) {
      try {
        const res = await fetch(`${API}/templates/${id}`);
        const t = await res.json();
        document.getElementById('templateName').value = t.name || '';
        document.getElementById('templateSubject').value = t.subject || '';
        document.getElementById('templateBody').value = t.body || '';
        document.getElementById('templateForm').style.display = 'block';
        document.getElementById('templateForm').dataset.editId = id;
      } catch (err) {
        alert('Failed to load template');
      }
    } else if (action === 'delete' && id && confirm('Delete this template?')) {
      try {
        await fetch(`${API}/templates/${id}`, { method: 'DELETE' });
        await loadTemplates();
      } catch (err) {
        alert('Failed to delete template');
      }
    }
  });

  // Jobs: view details / retry (event delegation).
  document.getElementById('jobsContainer').addEventListener('click', async e => {
    const btn = e.target.closest('button');
    if (!btn) return;
    const action = btn.dataset.action;
    const jobId = parseInt(btn.dataset.jobId, 10);
    if (!jobId || isNaN(jobId)) return;
    if (action === 'job-details') {
      try {
        await showJobDetails(jobId);
      } catch (err) {
        alert(err.message || 'Failed to load job details');
      }
    } else if (action === 'job-retry-failed') {
      try {
        await retryFailed(jobId);
      } catch (err) {
        alert(err.message || 'Retry failed');
      }
    }
  });

  // Job details modal close + background click.
  document.getElementById('jobDetailsClose').addEventListener('click', closeJobDetailsModal);
  document.getElementById('jobDetailsModal').addEventListener('click', e => {
    if (e.target.id === 'jobDetailsModal') closeJobDetailsModal();
  });

  // Job recipient filter tabs.
  document.getElementById('jobRecipientTabs').addEventListener('click', async e => {
    const tab = e.target.closest('button');
    if (!tab) return;
    const status = tab.dataset.status ?? '';
    if (!currentJobDetails?.id) return;
    document.querySelectorAll('#jobRecipientTabs .tab').forEach(t => t.classList.toggle('active', t === tab));
    const container = document.getElementById('jobRecipientsContainer');
    container.innerHTML = '<p class="muted">Loading...</p>';
    try {
      const rows = await fetchJobRecipients(currentJobDetails.id, status);
      renderRecipientsTable(rows);
    } catch (err) {
      container.innerHTML = `<p class="muted">${escapeHtml(err.message || 'Failed to load recipients')}</p>`;
    }
  });

  // Retry failed from within modal.
  document.getElementById('jobRetryFailedBtn').addEventListener('click', async () => {
    if (!currentJobDetails?.id) return;
    await retryFailed(currentJobDetails.id);
  });
}

function updateSendButtonState() {
  const batchId = document.getElementById('batchSelect').value;
  const templateId = document.getElementById('templateSelect').value;
  document.getElementById('sendBtn').disabled = !batchId || !templateId;
}

init();
