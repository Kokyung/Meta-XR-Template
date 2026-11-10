using UnityEngine;

public static class AudioUtility
{
    /// <summary>
    /// 클립을 처음부터 재생한다. 같은 클립이 재생 중이면 처음부터 다시 재생된다.
    /// </summary>
    public static void PlayClip(this AudioSource audioSource, AudioClip clip)
    {
        if (audioSource.clip != clip)
        {
            audioSource.clip = clip;
        }

        audioSource.Play();
    }

    public static void PlayInterval(this AudioSource audioSource, float fromSeconds, float toSeconds)
    {
        // SetScheduledEndTime은 PlayScheduled로 예약된 재생에 적용되므로 같은 dspTime 기준으로 예약한다.
        var startTime = AudioSettings.dspTime;

        audioSource.time = fromSeconds;
        audioSource.PlayScheduled(startTime);
        audioSource.SetScheduledEndTime(startTime + (toSeconds - fromSeconds));
    }
}
