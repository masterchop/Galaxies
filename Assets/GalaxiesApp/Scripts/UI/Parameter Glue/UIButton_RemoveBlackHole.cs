﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.GalaxySim {

  public class UIButton_RemoveBlackHole : UIButton {

    public override void OnPress() {
      GalaxyUIOperations.RemoveBlackHole();
    }

  }

}
