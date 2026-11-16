using UnityEngine;

[RequireComponent(typeof(NonPlayerCharacter))]
public class LunarWanderer : MonoBehaviour
{
    public static string GetDialogue()
    {
        if (MoonInventory.Delivered)
            return GameLocalization.T("wanderer_done");
        if (MoonInventory.CanDeliver)
            return GameLocalization.T("wanderer_ready");
        return string.Format(GameLocalization.T("wanderer_intro"), MoonInventory.Artifacts.Count);
    }

    public static void SpawnNearPlayer()
    {
        if (FindFirstObjectByType<LunarWanderer>() != null) return;
        var player = FindFirstObjectByType<PlayerController>();
        var sprite = Resources.Load<Sprite>("MoonItems/LunarWanderer");
        if (player == null || sprite == null) return;
        Vector2 origin = player.transform.position;
        Vector2 position = origin + Vector2.right * 3.2f;
        bool found = false;
        for (int i=0;i<48;i++)
        {
            float angle = (i % 12) * 30f * Mathf.Deg2Rad;
            float radius = 3.2f + (i / 12) * .6f;
            Vector2 candidate = origin + new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
            bool blocked = false;
            foreach(var collider in Physics2D.OverlapCircleAll(candidate,.55f))
                if(!collider.isTrigger) { blocked=true;break; }
            if(blocked) continue;
            position = candidate; found=true; break;
        }
        if(!found) { Debug.LogWarning("No free position near spawn for lunar wanderer."); return; }
        var npc = new GameObject(GameLocalization.T("wanderer_name"),typeof(SpriteRenderer),typeof(CircleCollider2D),typeof(NonPlayerCharacter),typeof(LunarWanderer));
        npc.layer=LayerMask.NameToLayer("NPC");
        npc.transform.position=position;
        var renderer=npc.GetComponent<SpriteRenderer>();
        renderer.sprite=sprite;
        var playerRenderer=player.GetComponent<SpriteRenderer>();
        float height=playerRenderer!=null?playerRenderer.bounds.size.y*1.2f:1.6f;
        npc.transform.localScale=Vector3.one*(height/Mathf.Max(.01f,sprite.bounds.size.y));
        renderer.sortingLayerID=playerRenderer!=null?playerRenderer.sortingLayerID:0;
        renderer.sortingOrder=playerRenderer!=null?playerRenderer.sortingOrder:5;
        var trigger=npc.GetComponent<CircleCollider2D>();
        trigger.isTrigger=true;
        trigger.radius=.5f/npc.transform.localScale.x;
        trigger.offset=new Vector2(0,.35f/npc.transform.localScale.x);
    }
}
