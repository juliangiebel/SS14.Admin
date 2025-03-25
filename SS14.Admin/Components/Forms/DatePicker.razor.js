

export class DatePicker {
    static init(ref, id)
    {
        return new DatePicker(ref, id);
    }

    constructor(ref, id)
    {
        this.ref = ref;
        this.id = id;
    }
}

window.DatePicker = DatePicker;
