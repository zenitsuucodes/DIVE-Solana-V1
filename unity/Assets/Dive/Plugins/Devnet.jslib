mergeInto(LibraryManager.library, {
 DiveEvent: function(payload){if(window.diveEvent)window.diveEvent(UTF8ToString(payload));}
});
