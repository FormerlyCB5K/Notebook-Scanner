package org.notebookscanner;

import java.util.Arrays;
import java.util.Collections;

public final class DeviceHistoryTest {
    static int passed;
    static void check(String expected, String actual) {
        if (!java.util.Objects.equals(expected, actual)) throw new AssertionError("Expected " + expected + ", got " + actual);
        passed++;
    }
    public static void main(String[] args) {
        check(null, DeviceHistory.startupTarget(null, null, Arrays.asList("headphones", "laptop")));
        check("laptop", DeviceHistory.startupTarget(null, "laptop", Arrays.asList("headphones", "laptop")));
        // A later failed/selected device does not replace the last actual connection.
        check("laptop-A", DeviceHistory.startupTarget("laptop-A", "laptop-B", Arrays.asList("laptop-B", "laptop-A")));
        // Unpairing the last connected laptop must not silently trust another choice.
        check(null, DeviceHistory.startupTarget("laptop-A", "laptop-B", Collections.singletonList("laptop-B")));
        check(null, DeviceHistory.startupTarget("laptop-A", "laptop-A", Collections.emptyList()));
        System.out.println("PASS: " + passed + " Android saved-device policy cases.");
    }
}
