//import dayjs from "../../wwwroot/lib/dayjs";

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
        this.daysContainer.addEventListener('click', e => console.log(e));
        this.popup = this.root.querySelector('.datepicker-container');
        this.popupButton = this.root.querySelector('.datepicker-toggle');

        this.popupButton.addEventListener('click', () =>
        {
            if (this.popup.classList.contains('hidden'))
            {
                this.showPopup()
            }
            else
            {
                this.hidePopup()
            }
        });

        this.root.querySelector('.cancel-button').addEventListener('click', () => this.hidePopup());
        this.root.querySelector('.ok-button').addEventListener('click', () => this.hidePopup());
        this.root.querySelector('.prev').addEventListener('click', () => this.prevMonth());
        this.root.querySelector('.next').addEventListener('click', () => this.nextMonth());

        this.fromDateInput = this.root.querySelector('.from-date-input');
        this.fromDateInput.addEventListener('change', () => this.setFromDate());
        this.fromTimeInput = this.root.querySelector('.from-time-input');
        this.fromTimeInput.addEventListener('change', () => this.setFromDate());
        this.toDateInput = this.root.querySelector('.to-date-input');
        this.toDateInput.addEventListener('change', () => this.setToDate());
        this.toTimeInput = this.root.querySelector('.to-time-input');
        this.toTimeInput.addEventListener('change', () => this.setToDate());

        this.updatePosition();

        document.addEventListener('scroll', () => this.updatePosition(),{
            capture: true,
            passive: true
        });

        window.addEventListener('resize', () => this.updatePosition(), { capture: true });

        this.rangeStart = options.rangeStart != null ? dayjs(options.rangeStart) : null;
        this.rangeEnd = options.rangeEnd != null ? dayjs(options.rangeEnd) : null;

        this.currentDisplayDate = this.rangeStart != null ? this.rangeStart : dayjs();
        this.updateDisplay();
    }

    showPopup()
    {
        this.updateDisplay()
        this.popup.classList.remove('hidden');
        this.updatePosition()
    }

    hidePopup()
    {
        this.popup.classList.add('hidden');
    }

    updatePosition()
    {
        if (this.popup.classList.contains('hidden'))
            return;

        FloatingUIDOM.computePosition(this.popupButton, this.popup, {
            placement: 'bottom',
            middleware: [
                FloatingUIDOM.offset(6),
                FloatingUIDOM.flip(),
                FloatingUIDOM.shift({padding: 5})
            ]
        }).then(({x, y, placement, middlewareData}) => {
            Object.assign(this.popup.style, {
                left: `${x}px`,
                top: `${y}px`
            });
        });
    }
    updateDisplay()
    {
        this.daysContainer.innerHTML = "";
        this.currentMonthButton.textContent = this.currentDisplayDate.toDate().toLocaleString('en-us', {month: 'long', year: 'numeric'});
        let numberOfDays =  DatePicker.getDaysInMonth(this.currentDisplayDate.toDate());

        for (let i = 0; i < numberOfDays; i++)
        {
            // (╯°□°)╯︵ ┻━┻
            const day = this.currentDisplayDate.date(i + 1);
            const isSelected = (!this.isNanOrNull(this.rangeStart) &&  day.isSameOrAfter(this.rangeStart, 'day'))
                                    && (!this.isNanOrNull(this.rangeEnd) && day.isSameOrBefore(this.rangeEnd, 'day'));

            this.daysContainer.innerHTML += `<div data-day="${day.format('YYYY-MM-DD')}" class="day ${isSelected ? 'day-selected' : ''} w-6 h-6 day-text-nudge text-center text-sm cursor-pointer">${i + 1}</div>`;
        }
    }

    static getDaysInMonth(date)
    {
        return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
    }

    prevMonth() {
        this.currentDisplayDate = this.currentDisplayDate.subtract(1, 'month');
        this.updateDisplay();
    }

    nextMonth() {
        this.currentDisplayDate = this.currentDisplayDate.add(1, 'month');
        this.updateDisplay();
    }

    setFromDate()
    {
        let date = dayjs(this.fromDateInput.valueAsDate).utc().hour(0);
        const time = this.fromTimeInput.valueAsNumber;

        if (date != null && time != null && !isNaN(time))
            date = date.millisecond(time);

        this.rangeStart = date;
        this.updateDisplay();
    }

    setToDate()
    {
        let date = dayjs(this.toDateInput.valueAsDate).utc().hour(0);
        const time = this.toTimeInput.valueAsNumber;

        if (date != null && time != null && !isNaN(time))
            date = date.millisecond(time);

        this.rangeEnd = date;
        this.updateDisplay();
    }

    /**
     * I don't fucking care anymore
     */
    isNanOrNull(date)
    {
        return date == null || isNaN(date.unix())
    }
}

window.DatePicker = DatePicker;
