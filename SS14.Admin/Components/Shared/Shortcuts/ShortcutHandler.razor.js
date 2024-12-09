const ShortcutRepeatDelay = 500

export class ShortcutHandler {

    // JsInterop go brrr
    static Create(dotnetReference)
    {
        return new ShortcutHandler(dotnetReference);
    }
    constructor(dotnetReference) {
        this.reference = dotnetReference;
        this.shortcuts = [];
        this.lastRepeat = Date.now();
        document.addEventListener('keydown', this.OnKeyDown.bind(this));
    }

    RegisterShortcut(shortcutKey)
    {
        this.shortcuts.push(shortcutKey);
        console.log('Registered shortcode')
    }

    /**
     *
     * @param {KeyboardEvent} e
     */
    OnKeyDown(e)
    {
        if (e.repeat)
        {
            const timeDiff = Date.now() - this.lastRepeat;
            if (timeDiff <= ShortcutRepeatDelay)
                return;

            this.lastRepeat = Date.now();
        }

        let key = '';

        if (e.composed)
        {
            key = e.ctrlKey ? 'ctrl+' : '';
            key += e.shiftKey ? 'shift+' : '';
            key += e.altKey ? 'alt+' : '';
        }

        key += e.code;

        if(!this.shortcuts.includes(key))
            return;

        this.reference.invokeMethodAsync('OnShortcut', key);
        e.preventDefault();
    }
}

window.ShortcutHandler = ShortcutHandler;
