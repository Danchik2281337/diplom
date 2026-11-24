const favorite = document.querySelector('[data-favorite]');
if (favorite) {
    favorite.addEventListener('submit', async event => {
        event.preventDefault();
        const button = favorite.querySelector('button');
        const feedback = favorite.querySelector('.favorite-feedback');
        button.disabled = true;
        feedback.textContent = '';
        try {
            const response = await fetch(favorite.action, {
                method: 'POST',
                body: new FormData(favorite),
                headers: {Accept: 'application/json'}
            });
            if (response.redirected) {
                location.href = response.url;
                return;
            }
            if (!response.ok) throw new Error();
            const result = await response.json();
            button.setAttribute('aria-pressed', String(result.saved));
            button.querySelector('span').textContent = result.saved ? 'В избранном' : 'В избранное';
            favorite.elements.save.value = String(!result.saved);
            feedback.textContent = result.saved ? 'Автомобиль сохранён в личном кабинете.' : 'Автомобиль убран из избранного.';
        } catch {
            feedback.textContent = 'Не удалось сохранить изменение. Попробуйте ещё раз.';
        } finally {
            button.disabled = false;
        }
    });
}

