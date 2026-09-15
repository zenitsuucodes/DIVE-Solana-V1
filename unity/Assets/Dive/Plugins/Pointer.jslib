mergeInto(LibraryManager.library, {
  DiveCapture: function(enabled) {
    if(!enabled) { if(document.pointerLockElement)document.exitPointerLock();return; }
    try { var request=Module.canvas.requestPointerLock();if(request&&request.catch)request.catch(function(){console.info('DIVE: mouse capture unavailable; hold right mouse to look.');}); }
    catch(error){console.info('DIVE: hold right mouse to look.');}
  },
  DiveCaptured: function(){return document.pointerLockElement===Module.canvas?1:0;}
});
