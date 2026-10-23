"""Scene handling shared by the review tools (2 scenes: Title and Game, see Assets/Scripts/Core/GameScenes.cs).

ensure_scene(code, 'Game') loads the Game scene if another scene is playing (it continues the player's save in memory,
without writing it) and turns automatic saving off for the rest of the Play session, so fixtures never overwrite the
player's save. finish(code) loads the Title scene, which discards the fixture game.
"""
import time

def ensure_scene(code, scene):
    if code('return Application.isPlaying?"yes":"no";') != 'yes':
        raise SystemExit('Press Play in Unity first (either scene).')
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
