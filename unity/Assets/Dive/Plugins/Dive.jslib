mergeInto(LibraryManager.library, {
  DiveTileClicked: function(x,z) {
    window.dispatchEvent(new CustomEvent('dive-tile',{detail:{x:x,z:z}}));
  }
});
