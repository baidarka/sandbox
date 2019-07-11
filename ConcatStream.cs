using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace My.Streams
{

    /// <summary>
    /// Readonly concatenation of multiple streams.
    /// </summary>
    /// <remarks>
    /// Not thread safe.
    /// Based on code by Hasan Khan
    /// http://stackoverflow.com/questions/3879152/how-do-i-concatenate-two-system-io-stream-instances-into-one
    /// 
    /// BJB:
    /// Fixed two bugs:
    /// - recursive read could overwrite buffer: now using bytesRead as offset during recursive read
    /// - number of bytesRead was not properly returned: now adding number of bytesRead to recursive read result
    /// </remarks>
    public class ConcatenatedStream : System.IO.Stream
    {

        #region Variables
        private Queue<Stream> streams;
        private bool disposed = false;
        private static string componentName = "ConcatenatedStream";
        #endregion Variables

        #region C-Tor
        public ConcatenatedStream(IEnumerable<Stream> streams)
        {
            if (streams == null)
                throw new ArgumentNullException("streams");
            Debug.WriteLine("c-tor Adding " + streams.Count() + " streams.", componentName);
            this.streams = new Queue<Stream>(streams);
        }
        #endregion C-Tor

        #region Properties
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion Properties

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", "Buffer cannot be null.");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Non-negative number required.");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Non-negative number required.");

            if (streams.Count == 0)
            {
                Debug.WriteLine("Streams.Count zero, returning 0.", componentName);
                return 0;
            }

            Debug.WriteLine("Peak.Read with count " + count, componentName);
            int bytesRead = streams.Peek().Read(buffer, offset, count);
            if (bytesRead < count)
            {
                Debug.WriteLine(bytesRead + " < " + count + ", dequeuieng...", componentName);
                streams.Dequeue().Dispose();

                Debug.WriteLine("  (bytesRead so far: " + bytesRead + ")", componentName);
                bytesRead = Read(buffer, bytesRead, count - bytesRead) + bytesRead; // use bytesRead as (buffer) offset, to prevent overwriting bytes in the buffer during multiple reads
                Debug.WriteLine("  (returned from recursion, therefore added bytesRead from previous read)", componentName);
            }

            Debug.WriteLine("Returning " + bytesRead, componentName);
            return bytesRead;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine("Dispose Start.", componentName);
            if (!disposed)
            {
                try
                {
                    if (disposing)
                    {
                        while (streams != null && streams.Count > 0)
                        {
                            streams.Dequeue().Dispose();
                            Debug.WriteLine("Dispose Dequeued and disposed stream.", componentName);
                        }
                        if (streams != null) streams = null;
                    }
                    this.disposed = true;
                    Debug.WriteLine("Dispose Disposed all streams and queue.", componentName);

                }
                catch (Exception e)
                {
                    Debug.WriteLine("Dispose Squelched exception: " + e.ToString(), componentName);

                }
                finally
                {
                    base.Dispose(disposing);
                }
                Debug.WriteLine("Dispose End.", componentName);

            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}
