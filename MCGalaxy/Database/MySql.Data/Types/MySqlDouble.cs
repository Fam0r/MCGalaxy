// Copyright (c) 2004-2008 MySQL AB, 2008-2009 Sun Microsystems, Inc.
//
// MySQL Connector/NET is licensed under the terms of the GPLv2
// <http://www.gnu.org/licenses/old-licenses/gpl-2.0.html>, like most 
// MySQL Connectors. There are special exceptions to the terms and 
// conditions of the GPLv2 as it is applied to this software, see the 
// FLOSS License Exception
// <http://www.mysql.com/about/legal/licensing/foss-exception.html>.
//
// This program is free software; you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation; version 2 of the License.
//
// This program is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License 
// for more details.
//
// You should have received a copy of the GNU General Public License along 
// with this program; if not, write to the Free Software Foundation, Inc., 
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Data;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace MySql.Data.Types
{

  internal struct MySqlDouble : IMySqlValue
  {
    private double mValue;
    private bool isNull;

    public MySqlDouble(bool isNull)
    {
      this.isNull = isNull;
      mValue = 0.0;
    }

    public MySqlDouble(double val)
    {
      this.isNull = false;
      mValue = val;
    }

    #region IMySqlValue Members

    public bool IsNull
    {
      get { return isNull; }
    }

    object IMySqlValue.Value
    {
      get { return mValue; }
    }

    public double Value
    {
      get { return mValue; }
    }

    Type IMySqlValue.SystemType
    {
      get { return typeof(double); }
    }

    string IMySqlValue.MySqlTypeName
    {
      get { return "DOUBLE"; }
    }

    void IMySqlValue.WriteValue(MySqlPacket packet, bool binary, object val)
    {
      double v = (val is double) ? (double)val : Convert.ToDouble(val);
      if (binary)
        packet.Write(BitConverter.GetBytes(v));
      else
        packet.WriteStringNoNull(v.ToString("R", CultureInfo.InvariantCulture));
    }

    IMySqlValue IMySqlValue.ReadValue(MySqlPacket packet, long length,
    bool nullVal)
    {
      if (nullVal)
        return new MySqlDouble(true);

      if (length == -1)
      {
        byte[] b = new byte[8];
        packet.Read(b, 0, 8);
        return new MySqlDouble(BitConverter.ToDouble(b, 0));
      }
      string s = packet.ReadString(length);
      double d;
      try
      {
        d = Double.Parse(s, CultureInfo.InvariantCulture);
      }
      catch (OverflowException)
      {
        // MySQL server < 5.5 can return values not compatible with
        // Double.Parse(), i.e out of range for double.

        if (s.StartsWith("-", StringComparison.Ordinal))
          d = double.MinValue;
        else
          d = double.MaxValue;
      }
      return new MySqlDouble(d);
    }

    #endregion

  }
}