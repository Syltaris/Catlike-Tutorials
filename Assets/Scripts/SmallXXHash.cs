/*
The algorithm works by storing hash bits in an accumulator, for which we need a uint field. This value gets initialized with a seed number, to which the prime E is added.
This is the first step of creating a hash, so we do this via a public constructor method with a seed parameter. We'll treat the seed as a uint,
but signed integers are typically used in code so an int parameter is more convenient.

XXHash32 works by consuming its input in portions of 32 bits, potentialy in parallel. Our small version only concerns itself with eating a single portion in isolation, for which we'll add a SmallXXHash.Eat method that has an int parameter and returns nothing. We'll treat the input data as uint again, multiply it with prime C, and then add it to the accumulator.
This will lead to integer overflows, but that's fine as we don't care about numerical interpretations of the data. So all operations are effectively modulo 232.
*/

public readonly struct SmallXXHash
{
    const uint primeA = 0b10011110001101110111100110110001;
    const uint primeB = 0b10000101111010111100101001110111;
    const uint primeC = 0b11000010101100101010111000111101;
    const uint primeD = 0b00100111110101001110101100101111;
    const uint primeE = 0b00010110010101100110011110110001;

    readonly uint accumulator;

    public SmallXXHash(uint accumulator)
    {
        this.accumulator = accumulator;
    }

    public static implicit operator SmallXXHash(uint accumulator) => new SmallXXHash(accumulator);

    /*
        The final step of the XXHash algorithm is to mix the bits of the accumulator, to spread the influence of all input bits around. This is known as an avalanche effect.
        This happens after all data is eaten and the final hash value is needed, so we'll do this when converting to uint.

        The avalanche value begins equal to the accumulator. It's shifted right 15 steps and then combined with its original value via the ^ bitwise XOR operator.
        After that it's multiplied with prime B.
        This process is done again, shifted right 13 steps, XOR-ed, and multiplied with prime C,
        and then again with 16 steps but without further multiplication.
    */
    public static implicit operator uint(SmallXXHash hash)
    {
        uint avalanche = hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= primeB;
        avalanche ^= avalanche >> 13;
        avalanche *= primeC;
        avalanche ^= avalanche >> 16;
        return avalanche;
    } //=> hash.accumulator; // operator uint -> cast-to-uint operator | implicit  ~ uint SmallXXHashVar -> implicit casting

    public static SmallXXHash Seed(int seed) => (uint)seed + primeE;

    public SmallXXHash Eat(int data) => RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;

    public SmallXXHash Eat(byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;

    /*
        This is only the first step of the eating process. After adding the value Eat has to rotate the bits of the accumulator to the left.
        Let's add a private static method for that, shifting some data by a given amount of steps. Begin by shifting all bits leftward with the << operator.

        Yes, and Burst is able to recognize this code and use the appropriate ROL instruction.
        However, there isn't a vectorized ROL instruction so it will be done with two shifts and a bitwise OR when vectorization is possible.
    */
    static uint RotateLeft(uint data, int steps) => (data << steps) | (data >> 32 - steps); // put back shifted bits to end
}
