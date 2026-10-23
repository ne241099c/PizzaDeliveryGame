"""Scene handling shared by the review tools (2 scenes: Title and Game, see Assets/Scripts/Core/GameScenes.cs).

ensure_scene(code, scene) first locks saving for the whole Play session (CampaignSaveManager.SessionSavingLocked, which
survives scene loads), then loads the scene if another one is playing. Fixtures can then never overwrite the player's
save, even if the Game scene is reloaded meanwhile. finish(code) pauses the fixture without changing scenes and compares protected files against actual backups.
"""
import time
import shutil
from datetime import datetime
from pathlib import Path

_backup=None
_save_directory=None

def _protect_files(code):
    global _backup,_save_directory
    if _backup is not None:
        return
    _save_directory=Path(code('return CampaignSaveFile.Directory;'))
    _backup=Path(__file__).resolve().parents[2]/"Library"/"ReviewBackups"/datetime.now().strftime("%Y%m%d-%H%M%S-review")
    _backup.mkdir(parents=True,exist_ok=True)
    for name in ("campaign.json","records.json"):
        source=_save_directory/name
        if source.exists():
            shutil.copy2(source,_backup/name)


def ensure_scene(code, scene):
    if code('return Application.isPlaying?"yes":"no";') != 'yes':
        raise SystemExit('Press Play in Unity first (either scene).')
    # Before anything else: no Game scene in this Play session may write the save, even after a reload.
    code('CampaignSaveManager.SessionSavingLocked=true;return "saving locked for this Play session";')
    _protect_files(code)
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
    code('if(!Application.isPlaying)return "not playing";CampaignSaveManager.SessionSavingLocked=true;var c=UnityEngine.Object.FindFirstObjectByType<GameClock>();if(c!=null)c.AutomaticTick=false;return "fixture paused; saving remains locked";')
    if _backup is None:
        return
    changed=[]
    for name in ("campaign.json","records.json"):
        original=_backup/name
        current=_save_directory/name
        before=original.read_bytes() if original.exists() else None
        after=current.read_bytes() if current.exists() else None
        if before!=after:
            changed.append(name)
    if changed:
        raise RuntimeError("Save files changed: "+", ".join(changed)+". Backup: "+str(_backup)+". No automatic restore performed.")
