﻿namespace shared {
    public class FrameRingBuffer<T> {
        public const int RING_BUFF_CONSECUTIVE_SET = 0;
        public const int RING_BUFF_NON_CONSECUTIVE_SET = 1;
        public const int RING_BUFF_FAILED_TO_SET = 2;
        int Ed;        // write index, open index
        int St;        // read index, closed index
        int EdFrameId;
        int StFrameId;
        int N;
        int Cnt;       // the count of valid elements in the buffer, used mainly to distinguish what "st == ed" means for "Pop" and "Get" methods
        T[] Eles;
        T foo; // for convenient clearance popping
        public FrameRingBuffer(int n) {
            Cnt = St = Ed = StFrameId = EdFrameId = 0;
            N = n;
            Eles = new T[n];
        }

        public bool Put(T item) {
            while (0 < Cnt && Cnt >= N) {
                // Make room for the new element
                Pop(out foo);
            }
            Eles[Ed] = item;
            EdFrameId++;
            Cnt++;
            Ed++;

            if (Ed >= N) {
                Ed -= N; // Deliberately not using "%" operator for performance concern

            }
            return true;
        }

        public bool Pop(out T holder) {
            // C# doesn't allow generic type to be pointer, i.e. List<int*> is not allowed, thus would use this "output holder pattern" to avoid "Pop" copying large instance of T when sizeof(T) is much larger than 1 word.
            if (0 == Cnt) {
                holder = default(T);
                return false;
            }
            holder = Eles[St];
            StFrameId++;
            Cnt--; St++;

            if (St >= N) {
                St -= N;
            }
            return true;
        }

        public void DryPut() {
            while (0 < Cnt && Cnt >= N) {
                // Make room for the new element
                Pop(out foo);
            }
            EdFrameId++;
            Cnt++;
            Ed++;

            if (Ed >= N) {
                Ed -= N; // Deliberately not using "%" operator for performance concern
            }
        }

        public int GetArrIdxByOffset(int offsetFromSt) {
            if (0 == Cnt || 0 > offsetFromSt) {
                return -1;
            }
            int arrIdx = St + offsetFromSt;

            if (St < Ed) {
                // case#1: 0...st...ed...N-1
                if (St <= arrIdx && arrIdx < Ed) {
                    return arrIdx;
                }
            }
            else {
                // if St >= Ed
                // case#2: 0...ed...st...N-1
                if (arrIdx >= N) {
                    arrIdx -= N;

                }
                if (arrIdx >= St || arrIdx < Ed) {
                    return arrIdx;

                }
            }

            return -1;
        }

        public bool GetByOffset(int offsetFromSt, out T holder) {
            int arrIdx = GetArrIdxByOffset(offsetFromSt);

            if (-1 == arrIdx) {
                holder = default(T);
                return true;
            }
            holder = Eles[arrIdx];
            return true; 
        }

        public bool GetByFrameId(int frameId, out T holder) {
            if (frameId >= EdFrameId || frameId < StFrameId) {
                holder = default(T);
                return false;
            }
            return GetByOffset(frameId - StFrameId, out holder);
        }

        // [WARNING] During a battle, frontend could receive non-consecutive frames (either renderFrame or inputFrame) due to resync, the buffer should handle these frames properly.
        public (int, int, int) SetByFrameId(T pItem, int frameId) {
            int oldStFrameId = StFrameId;
            int oldEdFrameId = EdFrameId;

            if (frameId < oldStFrameId) {
                return (RING_BUFF_FAILED_TO_SET, oldStFrameId, oldEdFrameId);
            }
            // By now "StFrameId <= frameId"
            if (oldEdFrameId > frameId) {
                int arrIdx = GetArrIdxByOffset(frameId - StFrameId);

                if (-1 != arrIdx) {
                    Eles[arrIdx] = pItem;
                    return (RING_BUFF_CONSECUTIVE_SET, oldStFrameId, oldEdFrameId);
                }
            }

            // By now "EdFrameId <= frameId"
            int ret = RING_BUFF_CONSECUTIVE_SET;

            if (oldEdFrameId < frameId) {
                St = Ed = 0;
                StFrameId = EdFrameId = frameId;
                Cnt = 0;
                ret = RING_BUFF_NON_CONSECUTIVE_SET;
            }

            // By now "EdFrameId == frameId"
            Put(pItem);


            return (ret, oldStFrameId, oldEdFrameId);
        }
        public void Clear() {
            while (0 < Cnt) {
                Pop(out foo);
            }
            St = Ed = StFrameId = EdFrameId = 0;
        }
    }
}
