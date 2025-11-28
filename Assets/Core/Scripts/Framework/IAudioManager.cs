/// <summary>
/// 音频管理器接口
/// 提供 BGM 和音效播放控制
/// </summary>
public interface IAudioManager
{
    void PlayBGM();
    void PauseBGM();
    void StopBGM();
    void SetBGMVolume(float volume);
}
