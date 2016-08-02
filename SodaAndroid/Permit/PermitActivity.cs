using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;

namespace SodaAndroid
{
  [Activity (Label = "PermitActivity")]
  public class PermitActivity : FragmentActivity
  {
    protected override void OnCreate (Bundle savedInstanceState)
    {
      base.OnCreate (savedInstanceState);
      SetContentView (Resource.Id.permitActivityLayout);
    }
  }
}

