# Wire protocol NS01

Transport: secure paired Classic RFCOMM. Custom service UUID `e286e3de-3194-4e95-ae08-7ad9f621aba0`. Android advertises; Windows connects. No COM-port allocation. UUID is not authentication.

1. Windows sends the four ASCII bytes `NS01`. Android has a 20-second handshake deadline and rejects any mismatch. Android also checks that the remote device is bonded and matches the user-selected laptop address.
2. For each photo Android sends: four-byte unsigned big-endian JPEG length, 32 raw SHA-256 digest bytes of the JPEG, then exactly that many JPEG bytes. Java `DataOutputStream.writeInt` and .NET `BinaryPrimitives.ReadUInt32BigEndian` agree for the bounded positive lengths.
3. Windows reads exact lengths despite fragmented/coalesced RFCOMM reads. Reject zero/over-8-MiB lengths before allocation, digest mismatch, unsupported JPEG header or dimensions. Accept baseline JPEG with each dimension at most 4096 and at most 12 million pixels; Android emits maximum 2400 long edge, quality 85.
4. Windows decodes and copies the image on its UI STA thread, retrying a temporarily busy clipboard up to ten attempts. On success it writes one byte `06` (ACK) and flushes. Android reports “Copied” only after reading that byte. ACK confirms clipboard acceptance, not that ChatGPT received or understood anything.
5. Continue on the same connection with another frame. One image in flight; no persistent queue, retry, resume, sequence ID or replay. A lost ACK can produce a sender failure after a successful copy. Future retry logic would require IDs and deduplication.

Windows allows indefinite idle wait for the first byte, then a 120-second whole-frame deadline. Android uses a 120-second whole-send-and-ACK watchdog that closes the Bluetooth socket. Closing/Pause also interrupts the queue and socket. After a protocol/clipboard/connection failure, both sides close the connection.

In version 0.2, Windows retries the same explicitly selected phone, with serial attempts and 2/4/8-second backoff capped at 8 seconds. Pause/closing cancels an in-flight attempt, receive, clipboard retry or backoff. Denied access ends automatic attempts rather than repeating consent requests. Android keeps its selected-peer listener available after a disconnect. A per-connection reader detects EOF even while no photo is being sent, and accepts ACK only while one is expected. Socket close wakes blocked reads/writes; the transfer queue is checked at 250 ms intervals so an idle disconnect can return to accepting.

Each pending photo is tied to its original socket and discarded if the connection changes. Both apps remember only the explicitly chosen bonded peer; they never substitute another paired device automatically. Stored device choice changes the app connection setup, not NS01 framing. There is no change to image encoding, security mode or delivery acknowledgement. Connections are retried; photos are not replayed.

Version 0.3 also permits the standard camera to open before a socket is connected. One prepared, never-attempted JPEG may wait in memory for the explicitly selected laptop's first ready connection. Once offered to that socket it leaves the waiting slot and follows the same no-replay rule. Pausing/changing the target/closing the app clears the waiting slot. No frame bytes or protocol version change.

The checksum is integrity/error detection inside an authenticated transport, not independent cryptographic peer authentication. Trust is OS pairing plus explicit selected peer and secure sockets. No insecure transport fallback is implemented. There is no HTML/text/filename/command execution in the payload.
