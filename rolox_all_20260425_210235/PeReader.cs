using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RoloxBuilder
{
    /// <summary>
    /// Lê o formato PE32/PE64 puro em C# — sem cl.exe, sem dumpbin, sem dependências externas.
    /// </summary>
    public class PeInfo
    {
        public string FilePath   { get; set; } = "";
        public string FileName   { get; set; } = "";
        public string Machine    { get; set; } = "";   // x86 / x64 / ARM64
        public bool   Is64Bit    { get; set; }
        public bool   IsDll      { get; set; }
        public string Subsystem  { get; set; } = "";
        public DateTime? TimeDateStamp { get; set; }

        public List<string> Exports  { get; set; } = new();
        public List<string> Imports  { get; set; } = new();
        public List<string> Sections { get; set; } = new();
        public List<string> Strings  { get; set; } = new();  // strings ASCII ≥ 6 chars
        public string Error  { get; set; } = "";
    }

    public static class PeReader
    {
        public static PeInfo Read(string path)
        {
            var info = new PeInfo { FilePath = path, FileName = Path.GetFileName(path) };
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                // ── DOS header ──────────────────────────────────
                if (br.ReadUInt16() != 0x5A4D) { info.Error = "Não é um arquivo PE válido."; return info; }
                fs.Seek(0x3C, SeekOrigin.Begin);
                uint peOffset = br.ReadUInt32();

                // ── PE signature ────────────────────────────────
                fs.Seek(peOffset, SeekOrigin.Begin);
                if (br.ReadUInt32() != 0x00004550) { info.Error = "Assinatura PE inválida."; return info; }

                // ── COFF header ─────────────────────────────────
                ushort machine      = br.ReadUInt16();
                ushort numSections  = br.ReadUInt16();
                uint   timestamp    = br.ReadUInt32();
                br.ReadUInt32(); // SymbolTablePtr
                br.ReadUInt32(); // NumSymbols
                ushort optHeaderSz  = br.ReadUInt16();
                ushort characteristics = br.ReadUInt16();

                info.Machine  = machine switch { 0x014C => "x86", 0x8664 => "x64", 0xAA64 => "ARM64", _ => $"0x{machine:X4}" };
                info.IsDll    = (characteristics & 0x2000) != 0;
                info.TimeDateStamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

                // ── Optional header ─────────────────────────────
                long optStart = fs.Position;
                ushort magic  = br.ReadUInt16();
                info.Is64Bit  = magic == 0x020B;

                // Pula até Subsystem
                int subsystemOffset = info.Is64Bit ? 68 : 68;
                fs.Seek(optStart + subsystemOffset, SeekOrigin.Begin);
                ushort subsystem = br.ReadUInt16();
                info.Subsystem = subsystem switch
                {
                    2 => "GUI (Windows)", 3 => "Console", 1 => "Native",
                    9 => "WinCE GUI", _ => $"0x{subsystem:X4}"
                };

                // Data directories — offset from optStart
                int ddOffset = info.Is64Bit ? 112 : 96;
                fs.Seek(optStart + ddOffset, SeekOrigin.Begin);
                uint exportRva  = br.ReadUInt32(); br.ReadUInt32();
                uint importRva  = br.ReadUInt32(); br.ReadUInt32();

                // ── Section headers ─────────────────────────────
                fs.Seek(optStart + optHeaderSz, SeekOrigin.Begin);
                var sections = new List<(string Name, uint VirtAddr, uint VirtSize, uint RawOffset, uint RawSize)>();
                for (int i = 0; i < numSections; i++)
                {
                    byte[] nameBytes = br.ReadBytes(8);
                    string sname = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                    uint vsize  = br.ReadUInt32();
                    uint vaddr  = br.ReadUInt32();
                    uint rsize  = br.ReadUInt32();
                    uint roff   = br.ReadUInt32();
                    br.ReadBytes(12); // relocs, linenums, counts
                    uint flags  = br.ReadUInt32();
                    string attrs = "";
                    if ((flags & 0x20) != 0) attrs += "CODE ";
                    if ((flags & 0x40) != 0) attrs += "DATA ";
                    if ((flags & 0x80) != 0) attrs += "BSS ";
                    if ((flags & 0x20000000) != 0) attrs += "EXEC ";
                    if ((flags & 0x40000000) != 0) attrs += "READ ";
                    if ((flags & 0x80000000) != 0) attrs += "WRITE";
                    info.Sections.Add($"{sname,-10} VA=0x{vaddr:X8}  Size=0x{vsize:X6}  [{attrs.Trim()}]");
                    sections.Add((sname, vaddr, vsize, roff, rsize));
                }

                // ── Helper: RVA → file offset ───────────────────
                long RvaToOffset(uint rva)
                {
                    foreach (var s in sections)
                        if (rva >= s.VirtAddr && rva < s.VirtAddr + s.VirtSize)
                            return s.RawOffset + (rva - s.VirtAddr);
                    return -1;
                }

                // ── Export table ────────────────────────────────
                if (exportRva != 0)
                {
                    long eoff = RvaToOffset(exportRva);
                    if (eoff >= 0)
                    {
                        fs.Seek(eoff, SeekOrigin.Begin);
                        br.ReadUInt32(); // flags
                        br.ReadUInt32(); // timestamp
                        br.ReadUInt32(); // version
                        uint nameRva    = br.ReadUInt32();
                        uint ordBase    = br.ReadUInt32();
                        uint numFuncs   = br.ReadUInt32();
                        uint numNames   = br.ReadUInt32();
                        uint funcRva    = br.ReadUInt32();
                        uint nameRvaArr = br.ReadUInt32();
                        uint ordArr     = br.ReadUInt32();

                        for (int i = 0; i < numNames; i++)
                        {
                            long noff = RvaToOffset(nameRvaArr) + i * 4;
                            if (noff < 0) continue;
                            fs.Seek(noff, SeekOrigin.Begin);
                            uint nrva = br.ReadUInt32();
                            long soff = RvaToOffset(nrva);
                            if (soff < 0) continue;
                            fs.Seek(soff, SeekOrigin.Begin);
                            info.Exports.Add(ReadCString(br));
                        }
                    }
                }

                // ── Import table ────────────────────────────────
                if (importRva != 0)
                {
                    long ioff = RvaToOffset(importRva);
                    if (ioff >= 0)
                    {
                        fs.Seek(ioff, SeekOrigin.Begin);
                        while (true)
                        {
                            uint iltRva  = br.ReadUInt32();
                            br.ReadUInt32(); br.ReadUInt32();
                            uint dllNameRva = br.ReadUInt32();
                            br.ReadUInt32();
                            if (dllNameRva == 0) break;
                            long dnoff = RvaToOffset(dllNameRva);
                            if (dnoff < 0) break;
                            long saved = fs.Position;
                            fs.Seek(dnoff, SeekOrigin.Begin);
                            string dllName = ReadCString(br);
                            info.Imports.Add(dllName);
                            fs.Seek(saved, SeekOrigin.Begin);
                        }
                    }
                }

                // ── Strings (ASCII ≥ 6 chars) ───────────────────
                // Varre a seção .rdata ou .data
                foreach (var sec in sections)
                {
                    if (sec.Name != ".rdata" && sec.Name != ".data") continue;
                    fs.Seek(sec.RawOffset, SeekOrigin.Begin);
                    byte[] buf = br.ReadBytes((int)Math.Min(sec.RawSize, 512 * 1024));
                    ExtractStrings(buf, info.Strings, 6, 200);
                    break;
                }
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }
            return info;
        }

        private static string ReadCString(BinaryReader br)
        {
            var sb = new StringBuilder();
            byte b;
            while ((b = br.ReadByte()) != 0) sb.Append((char)b);
            return sb.ToString();
        }

        private static void ExtractStrings(byte[] data, List<string> result, int minLen, int maxCount)
        {
            var sb = new StringBuilder();
            foreach (byte b in data)
            {
                if (b >= 0x20 && b < 0x7F)
                {
                    sb.Append((char)b);
                }
                else
                {
                    if (sb.Length >= minLen)
                    {
                        result.Add(sb.ToString());
                        if (result.Count >= maxCount) return;
                    }
                    sb.Clear();
                }
            }
        }
    }
}
