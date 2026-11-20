// Teammate 2 - Booking & Review Engine front-end helpers.
// The server re-validates everything; this script only makes the page friendlier.
(function () {
    'use strict';

    // ---------------------------------------------------------------- booking page
    const root = document.getElementById('bookingForm');
    if (root) {
        const slotsUrl = root.dataset.slotsUrl;
        const technicianId = root.dataset.technicianId;
        const hourlyRate = parseFloat(root.dataset.hourlyRate || '0');
        const dateInput = document.getElementById('BookingDate');
        const slotBox = document.getElementById('slotContainer');
        const slotMsg = document.getElementById('slotMessage');
        const startInput = document.getElementById('StartTimeValue');
        const submitBtn = document.getElementById('bookingSubmit');
        const totalEl = document.getElementById('bookingTotal');
        const hoursEl = document.getElementById('bookingHours');
        const serviceBoxes = () => Array.from(root.querySelectorAll('input[name="SelectedSpecialtyIds"]'));
        let previousChoice = startInput.value;
        let requestId = 0;

        const pad = n => String(n).padStart(2, '0');
        const toIso = d => d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
        const today = new Date();
        const max = new Date(); max.setDate(max.getDate() + parseInt(root.dataset.maxDays || '60', 10));
        dateInput.min = toIso(today);
        dateInput.max = toIso(max);

        function selectedCount() { return serviceBoxes().filter(b => b.checked).length; }

        function updateSummary() {
            const n = selectedCount();
            hoursEl.textContent = n + (n === 1 ? ' hour' : ' hours');
            totalEl.textContent = (n * hourlyRate).toLocaleString('en-US', { maximumFractionDigits: 2 }) + ' EGP';
            submitBtn.disabled = !(n > 0 && dateInput.value && startInput.value);
        }

        function showMessage(text, kind) {
            slotMsg.className = 'small mt-2 text-' + (kind || 'muted');
            slotMsg.textContent = text || '';
        }

        async function loadSlots() {
            const n = selectedCount();
            slotBox.innerHTML = '';
            startInput.value = '';
            updateSummary();

            if (!dateInput.value) { showMessage('Pick a date to see the available times.'); return; }
            if (n === 0) { showMessage('Select at least one service first.'); return; }

            const myRequest = ++requestId;
            showMessage('Checking availability...');
            try {
                const url = slotsUrl + '?technicianId=' + encodeURIComponent(technicianId) +
                    '&date=' + encodeURIComponent(dateInput.value) + '&services=' + n;
                const res = await fetch(url, { headers: { 'Accept': 'application/json' } });
                const data = await res.json();
                if (myRequest !== requestId) return; // a newer request replaced this one

                if (!data.success) { showMessage(data.message || 'Could not load time slots.', 'danger'); return; }
                if (!data.slots.length) { showMessage(data.message || 'No free time slots on this date.', 'danger'); return; }

                showMessage(data.slots.length + ' time slot(s) available. Choose one:', 'success');
                const grid = document.createElement('div');
                grid.className = 'slot-grid';
                data.slots.forEach((slot, i) => {
                    const wrap = document.createElement('div');
                    wrap.className = 'slot-option';
                    const input = document.createElement('input');
                    input.type = 'radio'; input.name = 'slotChoice'; input.id = 'slot' + i; input.value = slot.value;
                    const label = document.createElement('label');
                    label.htmlFor = input.id; label.textContent = slot.label;
                    if (slot.value === previousChoice) { input.checked = true; startInput.value = slot.value; }
                    input.addEventListener('change', () => { startInput.value = input.value; previousChoice = input.value; updateSummary(); });
                    wrap.appendChild(input); wrap.appendChild(label); grid.appendChild(wrap);
                });
                slotBox.appendChild(grid);
                updateSummary();
            } catch (e) {
                if (myRequest === requestId) showMessage('Could not load time slots. Please try again.', 'danger');
            }
        }

        dateInput.addEventListener('change', () => { previousChoice = ''; loadSlots(); });
        serviceBoxes().forEach(b => b.addEventListener('change', loadSlots));
        updateSummary();
        if (dateInput.value) loadSlots(); else showMessage('Pick a date to see the available times.');
    }

    // ---------------------------------------------------------------- review page
    const stars = document.getElementById('starInput');
    if (stars) {
        const text = document.getElementById('starText');
        const words = ['', 'Poor', 'Fair', 'Good', 'Very good', 'Excellent'];
        const refresh = () => {
            const checked = stars.querySelector('input:checked');
            text.textContent = checked ? checked.value + ' / 5 - ' + words[parseInt(checked.value, 10)] : 'Tap a star to rate';
        };
        stars.addEventListener('change', refresh);
        refresh();
    }

    // Confirm before cancelling
    document.querySelectorAll('form[data-confirm]').forEach(f => {
        f.addEventListener('submit', e => { if (!window.confirm(f.dataset.confirm)) e.preventDefault(); });
    });
})();
