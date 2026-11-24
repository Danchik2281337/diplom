// Side panel keeps a normal link as a fallback when JavaScript is unavailable.
const drawer = document.querySelector('.trip-drawer');
let panelRequest = 0;
document.addEventListener('click', async event => {
    const link = event.target.closest('[data-trip-panel]');
    if (!link || !drawer || event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) return;
    event.preventDefault();
    const request = ++panelRequest;
    const content = drawer.querySelector('[data-drawer-content]');
    content.textContent = 'Загружаем поездку…';
    if (!drawer.open) drawer.showModal();
    try {
        const url = new URL(link.href);
        url.searchParams.set('panel', 'true');
        const response = await fetch(url);
        if (request !== panelRequest || !drawer.open) return;
        if (!response.ok || response.redirected) {
            location.href = link.href;
            return;
        }
        const html = await response.text();
        if (request !== panelRequest || !drawer.open) return;
        content.innerHTML = html;
    } catch {
        if (request !== panelRequest || !drawer.open) return;
        content.replaceChildren();
        const retry = document.createElement('a');
        retry.href = link.href;
        retry.textContent = 'Не удалось загрузить панель. Открыть поездку отдельной страницей';
        content.append(retry);
    }
});
document.querySelector('[data-close-drawer]')?.addEventListener('click', () => drawer.close());
drawer?.addEventListener('click', event => {
    if (event.target === drawer && event.clientX < drawer.getBoundingClientRect().left) drawer.close();
});

// Local image previews never upload files before the inspection form is submitted.
let previews = [];
function clearPreviews() {
    for (const url of previews) URL.revokeObjectURL(url);
    previews = [];
}
drawer?.addEventListener('close', clearPreviews);
window.addEventListener('pagehide', clearPreviews);
document.addEventListener('change', event => {
    if (!event.target.matches('[data-photo-input]')) return;
    clearPreviews();
    const input = event.target;
    const target = input.closest('form').querySelector('.upload-preview');
    target.replaceChildren();
    const files = [...input.files];
    const valid = files.length <= 6 && files.every(file => file.size <= 5 * 1024 * 1024 && ['image/jpeg', 'image/png'].includes(file.type));
    input.setCustomValidity(valid ? '' : 'Выберите до 6 фотографий JPEG или PNG, каждая до 5 МБ.');
    if (!valid) {
        target.textContent = input.validationMessage;
        input.reportValidity();
        return;
    }
    for (const file of files) {
        const url = URL.createObjectURL(file);
        previews.push(url);
        const image = document.createElement('img');
        image.src = url;
        image.alt = file.name;
        target.append(image);
    }
});
document.addEventListener('change', event => {
    if (!event.target.matches('select[name="kind"]')) return;
    event.target.form.elements.note.required = event.target.value === 'DepositWithheld';
});

// Comparison selection survives navigation between catalogue categories.
let selected = [];
try {
    const stored = JSON.parse(localStorage.getItem('forma-comparison') || '[]');
    if (Array.isArray(stored)) selected = [...new Set(stored.filter(id => Number.isInteger(id) && id > 0))].slice(0, 3);
} catch { selected = []; }
const tray = document.querySelector('.compare-tray');
function updateComparison(message) {
    if (!tray) return;
    tray.hidden = selected.length === 0;
    tray.querySelector('[data-compare-count]').textContent = message || 'Выбрано автомобилей: ' + selected.length + ' из 3';
    const query = new URLSearchParams();
    const current = new URLSearchParams(location.search);
    for (const key of ['start', 'end', 'location']) {
        if (current.has(key)) query.set(key, current.get(key));
    }
    for (const id of selected) query.append('ids', id);
    tray.querySelector('[data-compare-link]').href = '/compare?' + query;
    for (const input of document.querySelectorAll('[data-compare-id]')) {
        input.checked = selected.includes(Number(input.dataset.compareId));
    }
    try { localStorage.setItem('forma-comparison', JSON.stringify(selected)); } catch { /* Selection remains in memory. */ }
}
document.addEventListener('change', event => {
    if (!event.target.matches('[data-compare-id]')) return;
    const input = event.target;
    const id = Number(input.dataset.compareId);
    if (input.checked && selected.length >= 3) {
        input.checked = false;
        updateComparison('Уже выбрано 3 автомобиля. Уберите один, чтобы добавить другой.');
        return;
    }
    selected = input.checked ? [...new Set([...selected, id])] : selected.filter(value => value !== id);
    updateComparison();
});
document.querySelector('[data-compare-clear]')?.addEventListener('click', () => {
    selected = [];
    updateComparison();
});
updateComparison();

document.addEventListener('click', event => {
    const button = event.target.closest('[data-gallery]');
    if (!button) return;
    const image = document.querySelector('#gallery-main');
    image.src = button.dataset.gallery;
    image.alt = button.dataset.galleryAlt;
    for (const other of document.querySelectorAll('[data-gallery]')) {
        other.setAttribute('aria-pressed', String(other === button));
    }
});

const success = document.querySelector('.booking-success');
if (success) {
    success.showModal();
    success.querySelector('[data-close-success]').addEventListener('click', () => success.close());
}

for (const form of document.querySelectorAll('.searchbar, .filters form')) {
    form.addEventListener('submit', () => {
        document.querySelector('.fleet')?.classList.add('searching');
        document.querySelector('.car-grid')?.setAttribute('aria-busy', 'true');
    });
}
window.addEventListener('pageshow', () => {
    document.querySelector('.fleet')?.classList.remove('searching');
    document.querySelector('.car-grid')?.removeAttribute('aria-busy');
});

// All calendar calculations use Minsk wall dates as UTC-like values without DST.
const calendar = document.querySelector('[data-schedule]');
if (calendar) {
    const form = document.querySelector('[data-quote]');
    const grid = calendar.querySelector('.availability-grid');
    const help = calendar.querySelector('.calendar-help');
    let month = new Date(form.elements.start.value.slice(0, 7) + '-01T00:00:00Z');
    if (Number.isNaN(month.getTime())) month = new Date(calendar.dataset.today.slice(0, 7) + '-01T00:00:00Z');
    let choosingEnd = false;
    let request = 0;
    let intervals = [];
    const key = date => date.toISOString().slice(0, 10);
    function render() {
        grid.replaceChildren();
        calendar.querySelector('[data-month-title]').textContent = month.toLocaleDateString('ru-RU', {month: 'long', year: 'numeric', timeZone: 'UTC'});
        for (const text of ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс']) {
            const label = document.createElement('small');
            label.textContent = text;
            grid.append(label);
        }
        const offset = (month.getUTCDay() + 6) % 7;
        for (let i = 0; i < offset; i++) grid.append(document.createElement('span'));
        const count = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + 1, 0)).getUTCDate();
        const start = form.elements.start.value.slice(0, 10);
        const end = form.elements.end.value.slice(0, 10);
        for (let day = 1; day <= count; day++) {
            const date = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth(), day));
            const next = new Date(date.getTime() + 86400000);
            const dateKey = key(date);
            const occupied = intervals.some(interval => interval.start < key(next) + 'T00:00' && interval.end > dateKey + 'T00:00');
            const button = document.createElement('button');
            button.type = 'button';
            button.textContent = day;
            button.disabled = dateKey < calendar.dataset.today;
            button.classList.toggle('occupied', occupied);
            button.classList.toggle('selected-day', dateKey >= start && dateKey <= end);
            button.setAttribute('aria-label', dateKey + (occupied ? ', есть занятые часы' : ', свободно'));
            button.addEventListener('click', () => {
                if (!choosingEnd) {
                    form.elements.start.value = dateKey + 'T' + (form.elements.start.value.slice(11) || '10:00');
                    const following = new Date(date.getTime() + 86400000);
                    form.elements.end.value = key(following) + 'T' + (form.elements.end.value.slice(11) || '10:00');
                    choosingEnd = true;
                    help.textContent = 'Теперь выберите день возврата.';
                } else {
                    if (dateKey <= form.elements.start.value.slice(0, 10)) {
                        help.textContent = 'Выберите день позже получения. Для аренды в один день задайте точное время в форме.';
                        return;
                    }
                    form.elements.end.value = dateKey + 'T' + (form.elements.end.value.slice(11) || '10:00');
                    choosingEnd = false;
                    help.textContent = 'Даты выбраны. Проверьте время и расчёт в форме.';
                }
                form.dispatchEvent(new Event('input', {bubbles: true}));
                render();
            });
            grid.append(button);
        }
    }
    async function loadMonth() {
        const version = ++request;
        help.textContent = 'Загружаем занятость…';
        grid.setAttribute('aria-busy', 'true');
        grid.replaceChildren();
        try {
            const response = await fetch('/api/cars/' + calendar.dataset.schedule + '/schedule?month=' + key(month));
            if (!response.ok) throw new Error();
            const result = await response.json();
            if (version !== request) return;
            intervals = result.intervals;
            render();
            help.textContent = 'Выберите день получения. Точки отмечают дни с занятыми часами.';
        } catch {
            if (version !== request) return;
            grid.replaceChildren();
            help.textContent = 'Не удалось загрузить календарь. Выберите даты в форме или переключите месяц для повтора.';
        } finally {
            if (version === request) grid.removeAttribute('aria-busy');
        }
    }
    for (const button of calendar.querySelectorAll('[data-month]')) {
        button.addEventListener('click', () => {
            const next = new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + Number(button.dataset.month), 1));
            if (next.getUTCFullYear() < 2000 || next.getUTCFullYear() > 2098) return;
            month = next;
            loadMonth();
        });
    }
    form.addEventListener('input', () => { if (grid.childElementCount) render(); });
    loadMonth();
}

