package org.notebookscanner;

import java.util.Collection;

/** Resolve a trusted ID independently of asynchronous dropdown layout callbacks. */
public final class DeviceHistory {
    private DeviceHistory() {}

    public static String startupTarget(String lastConnected, String legacyChoice, Collection<String> paired) {
        String candidate = lastConnected != null ? lastConnected : legacyChoice;
        return candidate != null && paired.contains(candidate) ? candidate : null;
    }
}
