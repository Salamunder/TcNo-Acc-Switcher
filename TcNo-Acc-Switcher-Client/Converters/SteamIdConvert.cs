﻿// TcNo Account Switcher - A Super fast account switcher
// Copyright (C) 2019-2021 TechNobo (Wesley Pyburn)
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Linq;

namespace TcNo_Acc_Switcher_Client.Converters
{
    internal class SteamIdConvert
    {
        // Usage:
        // - Console.WriteLine(new SteamIdConvert("STEAM_0:0:52161201").PrintAll());
        // - SteamIdConvert sid = new SteamIdConvert("STEAM_0:0:52161201");
        //   string Steam64 = sid.Id64;

        public string Id = "STEAM_0:", Id3 = "U:1:", Id32, Id64;

        private string _input;
        private byte _inputType;

        private static readonly char[] Sid3Strings = { 'U', 'I', 'M', 'G', 'A', 'P', 'C', 'g', 'T', 'L', 'C', 'a' };
        private const byte SteamId = 1, SteamId3 = 2, SteamId32 = 3, SteamId64 = 4;
        private const long ChangeVal = 76561197960265728;

        public SteamIdConvert(string anySteamId)
        {
            try
            {
                GetIdType(anySteamId);
                ConvertAll();
            }
            catch (SteamIdConvertException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void GetIdType(string sInput)
        {
            _input = sInput;
            if (_input[0] == 'S')
            {
                _inputType = 1; // SteamID
            }
            else if (Sid3Strings.Contains(_input[0]))
            {
                _inputType = 2; // SteamID3
            }
            else if (char.IsNumber(_input[0]))
            {
                if (_input.Length < 17)
                    _inputType = 3; // SteamID32
                else if (_input.Length == 17)
                    _inputType = 4; // SteamID64
            }
            else
            {
                throw new SteamIdConvertException("Input SteamID was not recognised!");
            }
        }

        private string GetOddity(string input)
        {
            return (int.Parse(input) % 2).ToString();
        }

        private string FloorDivide(string sIn, int divIn)
        {
            return Math.Floor((double)(int.Parse(sIn) / divIn)).ToString(CultureInfo.InvariantCulture);
        }

        private string CalcSteamId()
        {
            if (_inputType == SteamId)
            {
                Id = _input;
            }
            else
            {
                var s = "";
                switch (_inputType)
                {
                    case SteamId3:
                        s = _input.Substring(4);
                        break;
                    case SteamId32:
                        s = _input;
                        break;
                    case SteamId64:
                        s = CalcSteamId32();
                        break;
                }

                Id += GetOddity(s) + ":" + FloorDivide(s, 2);
            }

            return Id;
        }

        private string CalcSteamId3()
        {
            if (_inputType == SteamId3)
                Id3 = _input;
            else
                Id3 += CalcSteamId32();
            Id3 = $"[{Id3}]";
            return Id3;
        }

        private string CalcSteamId32()
        {
            if (_inputType == SteamId32)
            {
                Id32 = _input;
            }
            else
            {
                Id32 = _inputType switch
                {
                    SteamId => (int.Parse(_input.Substring(10)) * 2 + int.Parse($"{_input[8]}")).ToString(),
                    SteamId3 => _input.Substring(4),
                    SteamId64 => (long.Parse(_input) - ChangeVal).ToString(),
                    _ => Id32
                };
            }

            return Id32;
        }

        private string CalcSteamId64()
        {
            if (_inputType == SteamId64)
                Id64 = _input;
            else
                switch (_inputType)
                {
                    case SteamId:
                        Id64 =
                            (int.Parse(_input.Substring(10)) * 2 + int.Parse($"{_input[8]}") + ChangeVal).ToString();
                        break;
                    case SteamId3:
                        Id64 = (int.Parse(_input.Substring(4)) + ChangeVal).ToString();
                        break;
                    case SteamId32:
                        Id64 = (int.Parse(_input) + ChangeVal).ToString();
                        break;
                }

            return Id64;
        }


        public void ConvertAll()
        {
            CalcSteamId();
            CalcSteamId3();
            CalcSteamId32();
            CalcSteamId64();
        }

        public string PrintAll()
        {
            return $"SteamID: {Id}\nSteamID3: {Id3}\nSteamID32: {Id32}\nSteamID64: {Id64}";
        }

        private class SteamIdConvertException : Exception
        {
            public SteamIdConvertException(string message) : base(message)
            {
            }
        }
    }
}
