"""Scene handling shared by the review tools (2 scenes: Title and Game, see Assets/Scripts/Core/GameScenes.cs).

ensure_scene(code, scene) first locks saving for the whole Play session (CampaignSaveManager.SessionSavingLocked, which
survives scene loads), then loads the scene if another one is playing. Fixtures can then never overwrite the player's
save, even if the Game scene is reloaded meanwhile. finish(code) loads the Title scene, which discards the fixture game.
"""
import time

def ensure_scene(code, scene):
    if code('return Application.isPlaying?"yes":"no";') != 'yes':
        raise SystemExit('Press Play in Unity first (either scene).')
    # Before anything else: no Game scene in this Play session may write the save, even after a reload.
    code('CampaignSaveManager.SessionSavingLocked=true;return "saving locked for this Play session";')
    active = code('return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;')
    if active != scene:
        code('GameScenes.LoadGame(LaunchMode.Direct);return "load";' if scene == 'Game' else 'GameScenes.LoadTitle();return "load";')
        probe = 'CampaignSaveManager' if scene == 'Game' else 'TitleScene'
        for _ in range(50):
            time.sleep(.2)
            if code(f'return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="{scene}" && UnityEngine.Object.FindFirstObjectByType<{probe}>()!=null?"yes":"no";') == 'yes':
                break
        else:
            raise RuntimeError('scene did not load: ' + scene)
        time.sleep(.5)
    if scene == 'Game':
        code('UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>().AutomaticSavingEnabled=false;return "saving off for this Play session";')

def finish(code):
    code('GameScenes.LoadTitle();return "back to the title";')
