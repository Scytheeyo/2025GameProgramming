import os

# 삭제할 파일 목록 (사용자가 제공한 로그 기반)
files_to_delete = [
    "Assets/GFX/Animations/Barrier.meta",
    "Assets/GFX/Animations/Barrier/ManaShield.anim.meta",
    "Assets/GFX/Animations/Enemy/CardSoldier/Clover/Anim_CloverSpin.anim.meta",
    "Assets/GFX/Animations/Enemy/CardSoldier/Clover/Card3_Idle.anim.meta",
    "Assets/GFX/Animations/SkillAnimation.meta",
    "Assets/GFX/Animators/Barrier.controller.meta",
    "Assets/GFX/Animators/choicePanel.controller.meta",
    "Assets/GFX/Animators/Enemy.meta",
    "Assets/GFX/Animators/Enemy/Card1.controller.meta",
    "Assets/GFX/Animators/Enemy/Card2.controller.meta",
    "Assets/GFX/Animators/Enemy/Card3.controller.meta",
    "Assets/GFX/Animators/explosion_0.controller.meta",
    "Assets/GFX/Animators/slashspritesheet_0.controller.meta",
    "Assets/GFX/Sprites/Cards.meta",
    "Assets/GFX/Sprites/CastleBG.png.meta",
    "Assets/GFX/Sprites/Enemy/CardSordiers/Clover.meta",
    "Assets/GFX/Sprites/Enemy/CardSordiers/Clover/CardSordier3_Idle.png.meta",
    "Assets/GFX/Sprites/Enemy/CardSordiers/Heart/CardSordier2_heal.png.meta",
    "Assets/GFX/Sprites/Enemy/CheshierCat/CheshierCat_Attack.png.meta",
    "Assets/GFX/Sprites/Enemy/QweenOfSpade/QueenOfSpade_Walk.png.meta",
    "Assets/GFX/Sprites/Generated Image November 26, 2025 - 3_37PM-Photoroom - 복사본.png.meta",
    "Assets/GFX/Sprites/Portal.anim.meta",
    "Assets/GFX/Sprites/portalRings1_0.controller.meta",
    "Assets/GFX/Sprites/Skills/burning_start_1.png.meta",
    "Assets/GFX/Sprites/Skills/Smash.png.meta",
    "Assets/GFX/Sprites/Skills/SwordAuraSprite.png.meta",
    "Assets/GFX/Sprites/UI/FrameCardDownTable.png.meta",
    "Assets/GFX/Sprites/UI/FrameCardUpTable.png.meta",
    "Assets/GFX/Sprites/UI/InventorySlot.png.meta",
    "Assets/Prefabs/Caterpillar.prefab.meta",
    "Assets/Prefabs/diamond.prefab.meta",
    "Assets/Prefabs/ExplosionBullet.prefab.meta",
    "Assets/Prefabs/healer.prefab.meta",
    "Assets/Prefabs/QueenOfSpade.prefab.meta",
    "Assets/Prefabs/SceneFadeManager.prefab.meta",
    "Assets/Room.cs.meta",
    "Assets/Scripts/AutoDestroy.cs.meta",
    "Assets/Scripts/Boss_CardCaptain.cs.meta",
    "Assets/Scripts/Boss_HeartQueen.cs.meta",
    "Assets/Scripts/CardData.cs.meta",
    "Assets/Scripts/Confiner.cs.meta"
]

def delete_files():
    print("--- 파일 삭제 시작 ---")
    deleted_count = 0
    not_found_count = 0

    for file_path in files_to_delete:
        # 경로 구분자를 현재 운영체제에 맞게 변경 (Windows/Mac 호환)
        normalized_path = os.path.normpath(file_path)
        
        if os.path.exists(normalized_path):
            try:
                os.remove(normalized_path)
                print(f"[삭제됨] {normalized_path}")
                deleted_count += 1
            except Exception as e:
                print(f"[오류] {normalized_path} 삭제 실패: {e}")
        else:
            print(f"[없음] {normalized_path} 파일을 찾을 수 없습니다.")
            not_found_count += 1

    print("-" * 30)
    print(f"총 {len(files_to_delete)}개 중 {deleted_count}개 삭제 완료.")
    if not_found_count > 0:
        print(f"{not_found_count}개 파일은 이미 없거나 찾을 수 없습니다.")
    print("삭제가 완료되었습니다. Unity 에디터로 돌아가시면 자동으로 .meta 파일이 재생성됩니다.")

if __name__ == "__main__":
    # 안전 장치: 사용자 확인
    print("주의: 이 스크립트는 Unity .meta 파일들을 삭제합니다.")
    print("삭제 후 Unity에서 'Missing Reference'가 발생할 수 있습니다.")
    response = input("정말 진행하시겠습니까? (y/n): ")
    
    if response.lower() == 'y':
        delete_files()
    else:
        print("작업이 취소되었습니다.")
