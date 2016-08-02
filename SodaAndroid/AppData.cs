using System;

using static SodaDataProviderModule;

using static SodaServiceDefinition;

namespace SodaAndroid
{
  // This is just a nasty place to store a SodaDataProvider instance.
  public class AppData
  {
    public static SodaDataProvider sodaDataProvider = new SodaDataProvider ();

    public AppData ()
    {
    }
  }
}

