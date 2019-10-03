package net.jimblackler.istresser;

import android.app.Activity;
import android.app.ActivityManager;
import android.content.Context;
import android.os.Debug;

import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class Heuristics {
  private static List<Integer> getPids(Activity activity) {
    ActivityManager activityManager = (ActivityManager) Objects.requireNonNull(
        activity.getSystemService((Context.ACTIVITY_SERVICE)));
    List<Integer> pids = new ArrayList<>();
    List<ActivityManager.RunningAppProcessInfo> runningAppProcesses =
        activityManager.getRunningAppProcesses();
    for (ActivityManager.RunningAppProcessInfo runningAppProcessInfo : runningAppProcesses) {
      pids.add(runningAppProcessInfo.pid);
    }
    return pids;
  }

  static int getOomScore(Activity activity) {
    try {
      List<Integer> pids = getPids(activity);
      return Integer.parseInt(readFile(("/proc/" + pids.get(0)) + "/oom_score"));
    } catch (IOException | NumberFormatException e) {
      return 0;
    }
  }

  private static String readStream(InputStream inputStream) throws IOException {
    try (
        InputStreamReader inputStreamReader = new InputStreamReader(inputStream);
        BufferedReader reader = new BufferedReader(inputStreamReader)) {
      String newline = System.getProperty("line.separator");
      StringBuilder output = new StringBuilder();
      String line;
      while ((line = reader.readLine()) != null) {
        if (output.length() > 0) {
          output.append(newline);
        }
        output.append(line);
      }
      return output.toString();
    }
  }

  private static String readFile(String filename) throws IOException {
    return readStream(new FileInputStream(filename));
  }

  static ActivityManager.MemoryInfo getMemoryInfo(Activity activity) {
    ActivityManager activityManager = (ActivityManager) Objects.requireNonNull(
        activity.getSystemService((Context.ACTIVITY_SERVICE)));
    ActivityManager.MemoryInfo memoryInfo = new ActivityManager.MemoryInfo();
    activityManager.getMemoryInfo(memoryInfo);
    return memoryInfo;
  }

  static Map<String, Long> processMeminfo() {
    Map<String, Long> output = new HashMap<>();

    try {
      String meminfoText = readFile("/proc/meminfo");
      Pattern pattern = Pattern.compile("([^:]+)[^\\d]*(\\d+).*\n");
      Matcher matcher = pattern.matcher(meminfoText);
      while (matcher.find()) {
        output.put(matcher.group(1), Long.parseLong(Objects.requireNonNull(matcher.group(2))));
      }
    } catch (IOException e) {
      // Intentionally silenced
    }
    return output;
  }

  static boolean lowMemoryCheck(Activity activity) {
    return getMemoryInfo(activity).lowMemory;
  }

  static boolean oomCheck(Activity activity) {
    return getOomScore(activity) > 650;
  }

  static boolean commitLimitCheck() {
    Long value = processMeminfo().get("CommitLimit");
    if (value == null) {
      return false;
    }
    return Debug.getNativeHeapAllocatedSize() / 1024 > value;
  }

  static boolean availMemCheck(Activity activity) {
    ActivityManager.MemoryInfo memoryInfo = getMemoryInfo(activity);
    return memoryInfo.availMem < memoryInfo.threshold * 2;
  }
}
