package org.notebookscanner;

import android.content.Context;
import android.content.res.ColorStateList;
import android.graphics.*;
import android.graphics.drawable.*;
import android.view.*;
import android.widget.*;
import java.util.List;

public final class NightTheme extends Drawable {
    static final int TEXT=Color.rgb(237,238,248), PANEL=Color.rgb(27,32,48), ACCENT=Color.rgb(188,183,231);
    final Paint paint=new Paint(Paint.ANTI_ALIAS_FLAG);
    @Override public void draw(Canvas canvas) {
        float w=getBounds().width(),h=getBounds().height(); canvas.drawColor(Color.rgb(21,24,36));
        glow(canvas,w,h,.80f,.35f,0x42715278); glow(canvas,w,h,.15f,.30f,0x5043668c);
        paint.setShader(null);
        for(int i=0;i<28;i++) {
            int n=i%14; boolean right=i<14;
            float x=w*(right?.77f+(n*37%17)/100f:.13f+(n*23%13)/100f);
            float y=h*(right?.18f+(n*31%22)/100f:.60f+(n*19%18)/100f);
            paint.setColor(Color.argb(85+n*8,217,227,255)); canvas.drawCircle(x,y,.8f+(n%3)*.3f,paint);
        }
    }
    void glow(Canvas c,float w,float h,float x,float y,int color) {
        RadialGradient glow=new RadialGradient(0,0,1,color,Color.TRANSPARENT,Shader.TileMode.CLAMP);
        Matrix transform=new Matrix(); transform.setScale(w*.8f,h*.8f); transform.postTranslate(w*x,h*y); glow.setLocalMatrix(transform);
        paint.setShader(glow);
        c.drawRect(0,0,w,h,paint); paint.setShader(null);
    }
    @Override public void setAlpha(int alpha) {}
    @Override public void setColorFilter(ColorFilter filter) {}
    @Override public int getOpacity() { return PixelFormat.OPAQUE; }
    static GradientDrawable panel() { GradientDrawable d=new GradientDrawable();d.setColor(PANEL);d.setCornerRadius(12);d.setStroke(1,0x22ffffff);return d; }
    public static void style(View view) {
        if(view instanceof TextView) {
            TextView text=(TextView)view; text.setTextColor(TEXT);text.setTypeface(Typeface.create("sans-serif",Typeface.NORMAL)); text.setTextSize(15);
            text.setPadding(18,16,18,16);
            if(!(view instanceof Button)) view.setBackground(panel());
        }
        if(view instanceof Button || view instanceof Spinner || view instanceof Switch) {
            view.setBackground(panel());
            if(view instanceof Button) ((Button)view).setTextColor(ACCENT);
            if(view instanceof Switch) { ((Switch)view).setTextColor(TEXT); ((Switch)view).setThumbTintList(ColorStateList.valueOf(ACCENT)); ((Switch)view).setTrackTintList(ColorStateList.valueOf(0xff55516d)); }
        }
        if(view instanceof ViewGroup) for(int i=0;i<((ViewGroup)view).getChildCount();i++) style(((ViewGroup)view).getChildAt(i));
    }
    public static ArrayAdapter<String> adapter(Context context,List<String> names) {
        return new ArrayAdapter<String>(context,android.R.layout.simple_spinner_dropdown_item,names) {
            @Override public View getView(int p,View v,ViewGroup parent) { View result=super.getView(p,v,parent);style(result);result.setBackgroundColor(PANEL);return result; }
            @Override public View getDropDownView(int p,View v,ViewGroup parent) { View result=super.getDropDownView(p,v,parent);style(result);result.setBackgroundColor(PANEL);return result; }
        };
    }
}
