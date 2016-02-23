using System;
using System.IO;
using System.Text;

namespace LigerShark.TemplateBuilder.Tasks
{
    // TODO: We need to add logging to this class
    public class TokenReplacer : IDisposable
    {
        private readonly Stream _scratch;
        private readonly string _scratchFileName;
        private readonly Stream _source;
        private static readonly byte[] NoBytes = new byte[0];

        public TokenReplacer(Stream source)
            : this(source, CreateScratchStream())
        {
        }

        public TokenReplacer(Stream source, Stream scratch)
        {
            _source = source;
            _scratch = scratch;

            var scratchFileStream = _scratch as FileStream;

            if (scratchFileStream != null)
            {
                _scratchFileName = scratchFileStream.Name;
            }
        }

        /// <remarks>http://www.unicode.org/faq/utf_bom.html</remarks>
        public static Encoding DetectEncoding(Stream stream)
        {
            stream.Position = 0;
            var bomCandidate = new byte[4];
            var length = stream.Read(bomCandidate, 0, bomCandidate.Length);

            if (length == 0)
            {
                //File is zero length - pick something
                return Encoding.UTF8;
            }

            if (length >= 4)
            {
                if (bomCandidate[0] == 0x00 && bomCandidate[1] == 0x00 && bomCandidate[2] == 0xFE && bomCandidate[3] == 0xFF)
                {
                    //Big endian UTF-32
                    return Encoding.GetEncoding(12001);
                }

                if (bomCandidate[0] == 0xFF && bomCandidate[1] == 0xFE && bomCandidate[2] == 0x00 && bomCandidate[3] == 0x00)
                {
                    //Little endian UTF-32
                    return Encoding.UTF32;
                }
            }

            if (length >= 3)
            {
                if (bomCandidate[0] == 0xEF && bomCandidate[1] == 0xBB && bomCandidate[2] == 0xBF)
                {
                    //UTF-8
                    return Encoding.UTF8;
                }
            }

            if (length >= 2)
            {
                if (bomCandidate[0] == 0xFE && bomCandidate[1] == 0xFF)
                {
                    //Big endian UTF-16
                    return Encoding.BigEndianUnicode;
                }

                if (bomCandidate[0] == 0xFF && bomCandidate[1] == 0xFE)
                {
                    //Little endian UTF-16
                    return Encoding.Unicode;
                }
            }

            //Fallback to UTF-8
            return Encoding.UTF8;
        }

        public static bool ReplaceInFile(string fileName, string token, string replaceWith)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.Replace(token, replaceWith);
                }

                file.Flush();
            }

            return modified;
        }

        public static bool ReplaceInFile(string fileName, string token, string replaceWith, int codepage)
        {
            return ReplaceInFile(fileName, token, replaceWith, Encoding.GetEncoding(codepage));
        }

        public static bool ReplaceInFile(string fileName, string token, string replaceWith, string encodingName)
        {
            return ReplaceInFile(fileName, token, replaceWith, Encoding.GetEncoding(encodingName));
        }

        public static bool ReplaceInFile(string fileName, string token, string replaceWith, Encoding encoding)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.Replace(token, replaceWith, encoding);
                }

                file.Flush();
            }

            return modified;
        }

        public static bool ReplaceInFile(string fileName, byte[] tokenBytes, byte[] replaceWithBytes)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.Replace(tokenBytes, replaceWithBytes);
                }

                file.Flush();
            }

            return modified;
        }

        public static bool ProcessConditionalRegionsInFile(string fileName, string startToken, string endToken, bool isConditionSatisfied)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.ProcessConditionalRegion(startToken, endToken, isConditionSatisfied);
                }

                file.Flush();
            }

            return modified;
        }

        public static bool ProcessConditionalRegionsInFile(string fileName, string token, string replaceWith, int codepage)
        {
            return ReplaceInFile(fileName, token, replaceWith, Encoding.GetEncoding(codepage));
        }

        public static bool ProcessConditionalRegionsInFile(string fileName, string token, string replaceWith, string encodingName)
        {
            return ReplaceInFile(fileName, token, replaceWith, Encoding.GetEncoding(encodingName));
        }

        public static bool ProcessConditionalRegionsInFile(string fileName, string token, string replaceWith, Encoding encoding)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.Replace(token, replaceWith, encoding);
                }

                file.Flush();
            }

            return modified;
        }

        public static bool ProcessConditionalRegionsInFile(string fileName, byte[] tokenBytes, byte[] replaceWithBytes)
        {
            bool modified;

            using (var file = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var replacer = new TokenReplacer(file))
                {
                    modified = replacer.Replace(tokenBytes, replaceWithBytes);
                }

                file.Flush();
            }

            return modified;
        }

        public void Dispose()
        {
            try
            {
                _scratch.Dispose();

                if (!string.IsNullOrEmpty(_scratchFileName) && File.Exists(_scratchFileName))
                {
                    File.Delete(_scratchFileName);
                }
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public bool ProcessConditionalRegion(byte[] startTokenBytes, byte[] endTokenBytes, bool isConditionSatisfied)
        {
            if (isConditionSatisfied)
            {
                bool isModified = Replace(startTokenBytes, NoBytes);
                isModified |= Replace(endTokenBytes, NoBytes);
                return isModified;
            }

            _scratch.Position = 0;
            _scratch.SetLength(0);

            //Setup buffer
            const int defaultMinBufferSize = 16 * 1024 * 1024; //Try to read at least 16MB from the file at a time
            var requiredMinBufferSize = Math.Max(startTokenBytes.Length, endTokenBytes.Length);
            var bufferSize = Math.Max(requiredMinBufferSize, defaultMinBufferSize);
            var modified = false;

            //Reset source, get first buffer
            _source.Position = 0;
            var data = new byte[bufferSize];
            var realBufferLength = _source.Read(data, 0, data.Length);
            var bufferPosition = 0;
            var bytesWrittenToScratchSinceLastFlush = 0;
            var isReplacementPossible = true;
            bool isInOmitBlock = false;

            while (isReplacementPossible)
            {
                long moveBufferTo = -1;
                int matchIndex;
                byte[] searchFor = isInOmitBlock ? endTokenBytes : startTokenBytes;

                //Complete match
                if (FindPotentialMatchInBuffer(bufferPosition, searchFor, data, realBufferLength, out matchIndex))
                {
                    if (!isInOmitBlock)
                    {
                        //Write out the bytes leading to the match
                        _scratch.Write(data, bufferPosition, matchIndex - bufferPosition);
                        bytesWrittenToScratchSinceLastFlush += matchIndex - bufferPosition;
                    }

                    //Flip whether we're omitting the block
                    isInOmitBlock = !isInOmitBlock;
                    bufferPosition = matchIndex + searchFor.Length;
                    modified = true;
                }
                //Incomplete match
                else if (matchIndex > -1)
                {
                    //Calculate where we'd advance to (current position, less the current buffer size, plus the match index)
                    var advanceLocation = matchIndex + _source.Position - realBufferLength;
                    //If there are at least as many bytes left as are required to complete a token, replacement is possible, otherwise it's not
                    isReplacementPossible = _source.Length - advanceLocation >= searchFor.Length;

                    if (!isInOmitBlock)
                    {
                        //Write out the bytes leading to the potential match
                        _scratch.Write(data, bufferPosition, matchIndex - bufferPosition);
                        bytesWrittenToScratchSinceLastFlush += matchIndex - bufferPosition;
                    }
                    
                    //If we could complete the match, move the buffer to the index 
                    if (isReplacementPossible)
                    {
                        moveBufferTo = advanceLocation;
                    }
                }
                //No match, advance the buffer
                else
                {
                    if (!isInOmitBlock)
                    {
                        //Write out the remainder of the buffer
                        _scratch.Write(data, bufferPosition, realBufferLength - bufferPosition);

                        bytesWrittenToScratchSinceLastFlush += realBufferLength - bufferPosition;
                    }

                    moveBufferTo = _source.Position;
                }

                if (moveBufferTo > -1)
                {
                    _source.Position = moveBufferTo;
                    isReplacementPossible = _source.Position != _source.Length;

                    if (isReplacementPossible)
                    {
                        bufferPosition = 0;
                        realBufferLength = _source.Read(data, 0, data.Length);
                    }
                    else
                    {
                        bufferPosition = realBufferLength = 0;
                    }
                }

                if (bytesWrittenToScratchSinceLastFlush > bufferSize)
                {
                    _scratch.Flush();
                    bytesWrittenToScratchSinceLastFlush = 0;
                }
            }

            if (bufferPosition != realBufferLength)
            {
                _scratch.Write(data, bufferPosition, realBufferLength - bufferPosition);
            }

            if (modified)
            {
                _scratch.Flush();
                _scratch.Position = 0;
                _source.Position = 0;
                _scratch.CopyTo(_source);
                _source.SetLength(_scratch.Length);
            }

            return modified;
        }

        public bool Replace(byte[] tokenBytes, byte[] replacementBytes)
        {
            _scratch.Position = 0;
            _scratch.SetLength(0);

            //Setup buffer
            const int defaultMinBufferSize = 16 * 1024 * 1024; //Try to read at least 16MB from the file at a time
            var requiredMinBufferSize = Math.Max(tokenBytes.Length, replacementBytes.Length);
            var bufferSize = Math.Max(requiredMinBufferSize, defaultMinBufferSize);
            var modified = false;

            //Reset source, get first buffer
            _source.Position = 0;
            var data = new byte[bufferSize];
            var realBufferLength = _source.Read(data, 0, data.Length);
            var bufferPosition = 0;
            var bytesWrittenToScratchSinceLastFlush = 0;
            var isReplacementPossible = true;

            while (isReplacementPossible)
            {
                long moveBufferTo = -1;
                int matchIndex;
                //Complete match
                if (FindPotentialMatchInBuffer(bufferPosition, tokenBytes, data, realBufferLength, out matchIndex))
                {
                    //Write out the bytes leading to the match
                    _scratch.Write(data, bufferPosition, matchIndex - bufferPosition);

                    //Write out the replaced bytes
                    _scratch.Write(replacementBytes, 0, replacementBytes.Length);

                    bytesWrittenToScratchSinceLastFlush += matchIndex - bufferPosition + replacementBytes.Length;
                    bufferPosition = matchIndex + tokenBytes.Length;
                    modified = true;
                }
                //Incomplete match
                else if (matchIndex > -1)
                {
                    //Calculate where we'd advance to (current position, less the current buffer size, plus the match index)
                    var advanceLocation = matchIndex + _source.Position - realBufferLength;
                    //If there are at least as many bytes left as are required to complete a token, replacement is possible, otherwise it's not
                    isReplacementPossible = _source.Length - advanceLocation >= tokenBytes.Length;

                    //Write out the bytes leading to the potential match
                    _scratch.Write(data, bufferPosition, matchIndex - bufferPosition);

                    bytesWrittenToScratchSinceLastFlush += matchIndex - bufferPosition;

                    //If we could complete the match, move the buffer to the index 
                    if (isReplacementPossible)
                    {
                        moveBufferTo = advanceLocation;
                    }
                }
                //No match, advance the buffer
                else
                {
                    //Write out the remainder of the buffer
                    _scratch.Write(data, bufferPosition, realBufferLength - bufferPosition);

                    bytesWrittenToScratchSinceLastFlush += realBufferLength - bufferPosition;
                    moveBufferTo = _source.Position;
                }

                if (moveBufferTo > -1)
                {
                    _source.Position = moveBufferTo;
                    isReplacementPossible = _source.Position != _source.Length;

                    if (isReplacementPossible)
                    {
                        bufferPosition = 0;
                        realBufferLength = _source.Read(data, 0, data.Length);
                    }
                    else
                    {
                        bufferPosition = realBufferLength = 0;
                    }
                }

                if (bytesWrittenToScratchSinceLastFlush > bufferSize)
                {
                    _scratch.Flush();
                    bytesWrittenToScratchSinceLastFlush = 0;
                }
            }

            if (bufferPosition != realBufferLength)
            {
                _scratch.Write(data, bufferPosition, realBufferLength - bufferPosition);
            }

            if (modified)
            {
                _scratch.Flush();
                _scratch.Position = 0;
                _source.Position = 0;
                _scratch.CopyTo(_source);
                _source.SetLength(_scratch.Length);
            }

            return modified;
        }

        public bool Replace(string token, string replacement)
        {
            if (token == replacement)
            {
                return false;
            }

            // Console.WriteLine("Replacing {0} with {1}...", token, replacement);
            var encoding = DetectEncoding(_source);

            //Get byte runs for token and replacement
            var tokenBytes = encoding.GetBytes(token);
            var replacementBytes = encoding.GetBytes(replacement);

            return Replace(tokenBytes, replacementBytes);
        }

        public bool ProcessConditionalRegion(string startToken, string endToken, bool isConditionSatisfied)
        {
            // Console.WriteLine("{0} region {1} -> {2}...", isConditionSatisfied ? "Showing": "Hiding", startToken, endToken);
            var encoding = DetectEncoding(_source);

            //Get byte runs for token and replacement
            var startTokenBytes = encoding.GetBytes(startToken);
            var endTokenBytes = encoding.GetBytes(endToken);

            return ProcessConditionalRegion(startTokenBytes, endTokenBytes, isConditionSatisfied);
        }

        public bool Replace(string token, string replacement, Encoding encoding)
        {
            //Get byte runs for token and replacement
            var tokenBytes = encoding.GetBytes(token);
            var replacementBytes = encoding.GetBytes(replacement);

            return Replace(tokenBytes, replacementBytes);
        }

        private static Stream CreateScratchStream()
        {
            var tmpFile = Path.GetTempFileName();
            return File.Open(tmpFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        private static bool FindPotentialMatchInBuffer(int startingPosition, byte[] token, byte[] buffer, int realBufferLength, out int index)
        {
            for (var i = startingPosition; i < realBufferLength; ++i)
            {
                var isTokenMatch = true;
                int j;
                for (j = 0; isTokenMatch && j < token.Length && i + j < realBufferLength; ++j)
                {
                    isTokenMatch = token[j] == buffer[i + j];
                }

                if (isTokenMatch)
                {
                    index = i;
                    return j == token.Length;
                }
            }

            index = -1;
            return false;
        }
    }
}