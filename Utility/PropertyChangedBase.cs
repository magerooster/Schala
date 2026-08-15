using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Schala;

public class PropertyChangedBase : INotifyPropertyChanged, ICloneable
{
    #region Constructors
    //Default constructor
    public PropertyChangedBase()
    {
    }

    //Copy constructor
    public PropertyChangedBase(PropertyChangedBase Original, bool AsShallow)
    {
        //Not sure I want to actually clone the event's hooks... things get weird.
        //PropertyChanged = Original.PropertyChanged;
    }
    #endregion

    #region Implement INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void RaisePropertyChanged([CallerMemberName] string PropertyName = "")
    {
        RaisePropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
    }

    public virtual void RaisePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        sender ??= this;
        //if (args.PropertyName == "SourceInterfaceName" || args.PropertyName == "ActiveBatchElement")
        //    Debug.WriteLine("Property " + args.PropertyName + " changed on " + sender.GetType());
        PropertyChanged?.Invoke(sender, args);
    }
    #endregion
    #region Utility
    protected bool SetField<T>(ref T Field, T NewValue, [CallerMemberName] string PropertyName = "", bool ForceRaisePropertyChanged = false)
    {
        //Don't change it if it's already the same.
        if (!EqualityComparer<T>.Default.Equals(Field, NewValue))
        {
            Field = NewValue;

            RaisePropertyChanged(PropertyName);
            return true;
        }

        if (ForceRaisePropertyChanged)
            RaisePropertyChanged(PropertyName);
        return false;
    }

    protected bool SetField<T, TNew>(ref T Field, TNew NewValue, string UndoFieldName, string PropertyName, bool ForceRaisePropertyChanged)
        where T : class
        where TNew : class, T
    {
        return SetField(ref Field, NewValue, UndoFieldName, PropertyName, ForceRaisePropertyChanged);
    }

    //String has to be a special snowflake. ValueTypes work in the normal GetField fine. Reference types work fine. But STRING doesn't because it has no default constructor, due to being immutable.
    protected static string GetField(ref string Field)
    {
        Field ??= string.Empty;

        return Field;
    }
    protected static T GetField<T>(ref T Field) where T : new()
    {
        Field ??= new T();
        return Field;
    }
    #endregion
    #region Implement ICloneable
    public virtual object Clone()
    {
        return new PropertyChangedBase(this, false);
    }
    #endregion
}

public class ObservableBackgroundService(ILogger<SchalaClient> logger) : BackgroundService, INotifyPropertyChanged
{
    #region Implement INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void RaisePropertyChanged([CallerMemberName] string PropertyName = "")
    {
        RaisePropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
    }

    public virtual void RaisePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        sender ??= this;
        //if (args.PropertyName == "SourceInterfaceName" || args.PropertyName == "ActiveBatchElement")
        //    Debug.WriteLine("Property " + args.PropertyName + " changed on " + sender.GetType());
        PropertyChanged?.Invoke(sender, args);
    }
    #endregion
    #region Implement BackgroundService

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
    #endregion
    #region Utility
    protected bool SetField<T>(ref T Field, T NewValue, [CallerMemberName] string PropertyName = "", bool ForceRaisePropertyChanged = false)
    {
        //Don't change it if it's already the same.
        if (!EqualityComparer<T>.Default.Equals(Field, NewValue))
        {
            Field = NewValue;

            RaisePropertyChanged(PropertyName);
            return true;
        }

        if (ForceRaisePropertyChanged)
            RaisePropertyChanged(PropertyName);
        return false;
    }

    protected bool SetField<T, TNew>(ref T Field, TNew NewValue, string UndoFieldName, string PropertyName, bool ForceRaisePropertyChanged)
        where T : class
        where TNew : class, T
    {
        return SetField(ref Field, NewValue, UndoFieldName, PropertyName, ForceRaisePropertyChanged);
    }

    //String has to be a special snowflake. ValueTypes work in the normal GetField fine. Reference types work fine. But STRING doesn't because it has no default constructor, due to being immutable.
    protected static string GetField(ref string Field)
    {
        Field ??= string.Empty;

        return Field;
    }
    protected static T GetField<T>(ref T Field) where T : new()
    {
        Field ??= new T();
        return Field;
    }
    #endregion
}
