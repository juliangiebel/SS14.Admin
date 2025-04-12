

export class DatePicker {
    static init(ref, id, options)
    {
        return new DatePicker(ref, id, options);
    }

    constructor(ref, id, options)
    {
        this.ref = ref;
        this.id = id;
        this.root = document.getElementById(id);
        this.currentMonthButton = this.root.querySelector('.current-month');
        this.daysContainer = this.root.querySelector('.days-container');
        this.popup = this.root.querySelector('.datepicker-container');
        this.root.querySelector('.datepicker-toggle').addEventListener('click', () =>
        {
            this.popup.classList.toggle('hidden');
        });

        this.rangeStart = options.rangeStart != null ? new Date(options.rangeStart) : null;
        this.rangeEnd = options.rangeEnd != null ? new Date(options.rangeEnd) : null;

        this.currentDisplayDate = this.rangeStart != null ? this.rangeStart : new Date();
        this.updateDisplay();
    }

    updateDisplay()
    {
        this.currentMonthButton.textContent = this.currentDisplayDate.toLocaleString('en-us', {month: 'long'});
        let numberOfDays =  DatePicker.getDaysInMonth(this.currentDisplayDate);
        for (let i = 0; i < numberOfDays; i++)
        {
            this.daysContainer.innerHTML += `<div class="day">${i + 1}</div>`;
        }
    }

    static getDaysInMonth(date)
    {
        return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
    }
}

window.DatePicker = DatePicker;
