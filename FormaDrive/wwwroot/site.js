const quoteForm = document.querySelector('[data-quote]');
if (quoteForm) {
    let revision = 0;
    async function updateQuote() {
        const version = ++revision;
        const data = new FormData(quoteForm);
        const query = new URLSearchParams();
        for (const key of ['start', 'end', 'location', 'seat']) {
            if (data.has(key)) query.set(key, data.get(key));
        }
        const confirm = quoteForm.querySelector('[data-confirm]');
        const login = document.querySelector('#reserve-login');
        const error = document.querySelector('#quote-error');
        const alternatives = document.querySelector('#alternatives');
        alternatives.hidden = true;
        if (confirm) confirm.disabled = true;
        if (login) login.removeAttribute('href');
        const total = document.querySelector('#total');
        total.textContent = '…';
        try {
            const response = await fetch('/api/cars/' + quoteForm.dataset.quote + '/quote?' + query);
            const quote = await response.json();
            if (version !== revision) return;
            if (!response.ok) throw new Error(quote.message || 'Не удалось рассчитать стоимость.');
            document.querySelector('#rental').textContent = quote.rate + ' × ' + quote.days + ' сут.';
            document.querySelector('#extras').textContent = quote.extras + ' BYN';
            total.textContent = quote.total + ' BYN';
            quoteForm.elements.expected.value = quote.total;
            if (confirm) confirm.disabled = false;
            if (login) login.href = '/account/login?' + new URLSearchParams({returnUrl: location.pathname + '?' + query});
            error.hidden = true;
        } catch (e) {
            if (version !== revision) return;
            total.textContent = '—';
            error.textContent = e.message || 'Проверьте соединение и выбранные даты.';
            error.hidden = false;
            alternatives.href = '/?' + query + '#fleet';
            alternatives.hidden = false;
        }
    }
    quoteForm.addEventListener('input', updateQuote);
    updateQuote();
}

for (const form of document.querySelectorAll('.extend')) {
    form.addEventListener('input', () => {
        const end = form.elements.until.value;
        if (!end || !form.elements.until.validity.valid) {
            form.querySelector('output').textContent = '';
            return;
        }
        // Minsk wall-clock values share a fixed offset year-round.
        const days = Math.ceil((Date.parse(end + 'Z') - Date.parse(form.dataset.start + 'Z')) / 86400000);
        const total = days * Number(form.dataset.rate) + Number(form.dataset.extras);
        form.querySelector('output').textContent = 'Доплата ' + (total - Number(form.dataset.original)) + ' BYN · Всего ' + total + ' BYN';
    });
}
