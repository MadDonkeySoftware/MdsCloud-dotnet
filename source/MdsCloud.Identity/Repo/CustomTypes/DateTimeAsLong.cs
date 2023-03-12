using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace MdsCloud.Identity.Repo.CustomTypes;

public class DateTimeAsLong : IUserType
{
    private static long ObjectToLong(object value)
    {
        if (value.GetType() != typeof(DateTime))
            throw new ArgumentException("Value is not a date time");
        var date = (DateTime)value;

        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        var diff = date.ToUniversalTime() - origin;

        // NOTE: we convert to millisecond epoch UTC so fast operations can still be sorted by time stamps. Epoch is
        // typically seconds but the millisecond deviation is something that is generally accepted.
        return (long)Math.Floor(diff.TotalMilliseconds);
    }

    private static DateTime ObjectToDatetime(object value)
    {
        if (value.GetType() != typeof(long))
            throw new ArgumentException("Value is not a long");
        var msTimestamp = (long)value;
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        // NOTE: we convert to millisecond epoch UTC so fast operations can still be sorted by time stamps. Epoch is
        // typically seconds but the millisecond deviation is something that is generally accepted.
        return origin.AddMilliseconds(msTimestamp).ToUniversalTime();
    }

    public SqlType[] SqlTypes
    {
        get { return new[] { SqlTypeFactory.Int64 }; }
    }

    public Type ReturnedType => typeof(DateTime);

    public bool IsMutable => false;

    bool IUserType.Equals(object? x, object? y)
    {
        if (x == null && y != null)
            return false;
        if (x != null && y == null)
            return false;
        if (x == null && y == null)
            return true;
        return x.Equals(y);
    }

    public int GetHashCode(object? x)
    {
        return x == null ? 0 : x.GetHashCode();
    }

    public object? NullSafeGet(
        DbDataReader rs,
        string[] names,
        ISessionImplementor session,
        object owner
    )
    {
        var obj = NHibernateUtil.Int64.NullSafeGet(rs, names, session);
        return obj == null ? null : ObjectToDatetime(obj);
    }

    public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
    {
        if (cmd == null)
            throw new ArgumentException("DB Command cannot be null", nameof(cmd));

        if (value == null)
        {
            ((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
        }
        else
        {
            ((IDataParameter)cmd.Parameters[index]).Value = ObjectToLong(value);
        }
    }

    public object DeepCopy(object value)
    {
        // TODO: validate
        return value;
    }

    public object Replace(object original, object target, object owner)
    {
        // TODO: validate
        return original;
    }

    public object Assemble(object cached, object owner)
    {
        // TODO: validate
        return cached;
    }

    public object Disassemble(object value)
    {
        // TODO: validate
        return value;
    }
}
